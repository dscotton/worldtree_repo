Worldtree is a 2d platform game originally written for the National
Game Development Month game jam in 2012.

Original Version
================

Worldtree was written using PyGame. The PyGame version is playable,
assuming you have cloned the repo, with:
```
cd worldtree
python -m venv venv
source venv/bin/activate
pip install --upgrade pip
pip install pygame
python worldtree.py
```

Raylib-cs version
=================

In 2026 the project was ported to C# with the Raylib-cs library by AI
agents. The initial port was intended to be as close to identical to
the Python version as possible.

Requires .NET 9 SDK. The `media/` directory is a symlink to the
original Python assets.
`data/`.

```
cd worldtree-raylib/src
dotnet run
```
