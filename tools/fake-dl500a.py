#!/usr/bin/env python3
"""A fake Gigaset DL500A on a pseudo-terminal, for trying Freizeichen without the phone.

It answers the AT commands Freizeichen sends the way the DL500A does over Bluetooth SPP, and
prints every line it receives, so you can see exactly what a tel: link put on the wire.

    python3 tools/fake-dl500a.py                     # prints the port to choose in Freizeichen
    python3 tools/fake-dl500a.py --link /dev/rfcomm9 --scenario silent

--link makes the port appear in Freizeichen's port list (Linux lists /dev/rfcomm*, macOS
/dev/cu.*). Linking into /dev needs root, so the script calls sudo for that step alone.
Linux only so far; run Freizeichen with `make run-fake` so it can open the pty.
"""

import argparse
import os
import re
import subprocess
import sys
import tty

# What the phone answers to ATD, per scenario. "silent" answers nothing at all, not even AT: that
# is what the DL500A does while the PC also holds its Hands-Free audio connection.
DIAL_ANSWERS = {
    "ok": ["OK", "CLIP:EXTERN", "CONNECT"],
    "error": ["ERROR"],
    "busy": ["BUSY"],
    "nocarrier": ["NO CARRIER"],
    "nodialtone": ["NO DIALTONE"],
    "silent": [],
}


def answer(command, scenario):
    if scenario == "silent":
        return []
    if command == "AT":
        return ["OK"]
    if command == "AT+CGMI":
        return ["Gigaset Communications GmbH", "OK"]
    if command.startswith("ATD"):
        return DIAL_ANSWERS[scenario]
    return ["ERROR"]


def check(command):
    # Anything after ATD other than digits is what TelUri must never let through.
    if command.startswith("ATD") and not re.fullmatch(r"ATD \d+", command):
        print(f"!!! not just digits after ATD: {command!r}", flush=True)
    elif not command.startswith("AT"):
        print(f"!!! not an AT command: {command!r}", flush=True)


def link(target, name):
    try:
        if os.path.lexists(name):
            os.remove(name)
        os.symlink(target, name)
    except PermissionError:
        subprocess.run(["sudo", "ln", "-sfn", target, name], check=True)


def unlink(name):
    try:
        os.remove(name)
    except PermissionError:
        subprocess.run(["sudo", "rm", "-f", name], check=False)
    except FileNotFoundError:
        pass


def serve(master, scenario):
    pending = b""
    while True:
        try:
            chunk = os.read(master, 1024)
        except OSError:
            # Every slave fd closed; cannot happen while we hold ours, but end cleanly if it does.
            return
        pending += chunk
        *lines, pending = re.split(rb"\r\n|\r|\n", pending)
        for raw in lines:
            if not raw:
                continue
            command = raw.decode("ascii", errors="backslashreplace")
            print(f"<- {command!r}", flush=True)
            check(command)
            for reply in answer(command.strip(), scenario):
                print(f"-> {reply}", flush=True)
                os.write(master, f"\r\n{reply}\r\n".encode("ascii"))


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--scenario", choices=DIAL_ANSWERS, default="ok", help="how the phone answers ATD (default: ok)")
    parser.add_argument("--link", metavar="PATH", help="also expose the port under this name, e.g. /dev/rfcomm9")
    args = parser.parse_args()

    master, slave = os.openpty()
    # Holding the slave open keeps the pty alive between dials, and raw mode stops it echoing.
    tty.setraw(slave)
    port = os.ttyname(slave)
    if args.link:
        link(port, args.link)
        port = args.link

    print(f"Fake DL500A ({args.scenario}) on {port} – choose it in Freizeichen. Ctrl+C ends.", flush=True)
    try:
        serve(master, args.scenario)
    except KeyboardInterrupt:
        pass
    finally:
        if args.link:
            unlink(args.link)


if __name__ == "__main__":
    sys.exit(main())
