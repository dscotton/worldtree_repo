# Script used to build an executable version of the game with cx_Freeze.

import glob
import os
import cx_Freeze

executables = [cx_Freeze.Executable("worldtree.py")]
files = glob.glob(f"{os.getcwd()}\\*.py")
files.extend([f"{os.getcwd()}\\media\\"])
files.extend([f"{os.getcwd()}\\characters\\"])

cx_Freeze.setup(
    name="Worldtree",
    description="Worldtree Game",
    options={"build_exe": {"packages": ["pygame"],
                           "include_files": files}},
    executables=executables
)
