// LD_PRELOAD shim for trying Freizeichen against tools/fake-dl500a.py on Linux.
//
// A pseudo-terminal has no modem control lines, so the TIOCM* ioctls fail with ENOTTY and
// .NET's SerialPort refuses to open it with DtrEnable/RtsEnable set. This shim lets exactly those
// calls succeed when the device rejects them, and reports carrier, DSR and CTS as up. Every other
// ioctl, and every TIOCM* call on a real serial port, goes through unchanged.
//
//   gcc -shared -fPIC -o pty-modem-lines.so pty-modem-lines.c -ldl
//   LD_PRELOAD=$PWD/pty-modem-lines.so dotnet run --project src/Freizeichen.App

#define _GNU_SOURCE
#include <dlfcn.h>
#include <errno.h>
#include <stdarg.h>
#include <sys/ioctl.h>

int ioctl(int fd, unsigned long request, ...)
{
    static int (*real_ioctl)(int, unsigned long, ...);
    if (!real_ioctl)
        real_ioctl = (int (*)(int, unsigned long, ...))dlsym(RTLD_NEXT, "ioctl");

    va_list args;
    va_start(args, request);
    void *arg = va_arg(args, void *);
    va_end(args);

    int result = real_ioctl(fd, request, arg);
    if (result == 0 || (errno != ENOTTY && errno != EINVAL))
        return result;

    switch (request) {
    case TIOCMGET:
        *(int *)arg = TIOCM_CAR | TIOCM_DSR | TIOCM_CTS | TIOCM_DTR | TIOCM_RTS;
        return 0;
    case TIOCMSET:
    case TIOCMBIS:
    case TIOCMBIC:
        return 0;
    default:
        return result;
    }
}
