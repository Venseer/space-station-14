#!/usr/bin/env python3
import os
import sys
import urllib.request
import shutil

CURRENT_VERSION = "noise_0.0.1"
RELEASES_ROOT = "https://github.com/space-wizards/space-station-14/releases/download/" \
                + CURRENT_VERSION + "/"
WINDOWS_FILENAME = "ss14_noise-x86_64-pc-windows-msvc.dll"
MACOS_FILENAME = "libss14_noise-x86_64-apple-darwin.dylib"
LINUX_FILENAME = "libss14_noise-x86_64-unknown-linux-gnu.so"

WINDOWS_TARGET_FILENAME = "ss14_noise.dll"
LINUX_TARGET_FILENAME = "libss14_noise.so"
MACOS_TARGET_FILENAME = "libss14_noise.dylib"


def main():
    platform = sys.argv[1]
    target_os = sys.argv[2]
    # Hah good luck passing something containing a space to the Exec MSBuild Task.
    target_dir = " ".join(sys.argv[3:])

    if platform != "x64":
        print("Error: Unable to download ss14_noise for any platform outside x64. "
              "If you REALLY want x86 support for some misguided reason, I'm not providing it.")
        exit(1)

    repo_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
    dependencies_dir = os.path.join(repo_dir, "Dependencies", "ss14_noise")
    version_file = os.path.join(dependencies_dir, "VERSION")
    os.makedirs(dependencies_dir, exist_ok=True)

    existing_version = "?"
    if os.path.exists(version_file):
        with open(version_file, "r") as f:
            existing_version = f.read().strip()

    if existing_version != CURRENT_VERSION:
        for x in os.listdir(dependencies_dir):
            os.remove(x)

    with open(version_file, "w") as f:
        f.write(CURRENT_VERSION)

    filename = None
    target_filename = None

    if target_os == "Windows":
        filename = WINDOWS_FILENAME
        target_filename = WINDOWS_TARGET_FILENAME

    elif target_os == "Linux":
        filename = LINUX_FILENAME
        target_filename = LINUX_TARGET_FILENAME

    elif target_os == "MacOS":
        filename = MACOS_FILENAME
        target_filename = MACOS_TARGET_FILENAME

    else:
        print("Error: Unknown platform target:", target_os)
        exit(2)

    dependency_path = os.path.join(dependencies_dir, filename)
    if not os.path.exists(dependency_path):
        urllib.request.urlretrieve(RELEASES_ROOT + filename, dependency_path)

    target_file_path = os.path.join(target_dir, target_filename)

    if not os.path.exists(target_file_path) or \
       os.stat(dependency_path).st_mtime > os.stat(target_file_path).st_mtime:
        shutil.copy2(dependency_path, target_file_path)


if __name__ == '__main__':
    main()
