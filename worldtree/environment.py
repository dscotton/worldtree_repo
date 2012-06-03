"""
This class represents a game environment.

Created on Jun 3, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os
import sys

import pygame

import tile
import worldtree

TILE_SIZE = (32, 32)
MAPS_PATH = os.path.join('media', 'maps')

# TODO: Add images here as the second argument.
# TODO: First create tiles outside of this map (to load the surfaces), THEN map them to
# characters here.
TILES = {
  '-' : tile.Tile(True),
  ' ' : tile.Tile(False)
}
EMPTY_TILE = tile.Tile(False)

ENEMIES = {
}

class Environment(object):
  """A game environment.
  
  Attributes:
    map: A two-dimensional array of map tiles.
    screen_offset: (x, y) pixel offset of the upper-right corner of the current screen
      from the upper-right corner of the map.
  """
  
  def __init__(self, ascii_map):
    """Constructor.
    
    Args:
      ascii_map: String representation of the area ascii_map.  Each character represents a single tile,
        with the meaning as defined above.
    """
    # This is a little convoluted because in order to address tiles as [x][y] (rather than
    # [y][x]) we need to build a list of columns rather than a list of rows.
    self.map = []
    row = 0
    for line in ascii_map.splitlines():
      col = 0
      for char in line:
        if row == 0:
          # Extend the map to width = col.
          self.map.append([])
        if char in TILES:
          self.map[col].append(TILES[char])
        else:
          self.map[col].append(EMPTY_TILE)
        if char in ENEMIES:
          # TODO: implement this
          pass
        col += 1
      row += 1


def TestMap():
  """Simple test for this class - load "test.map" and validate."""
  pygame.display.set_caption("Map Test")
  screen = pygame.display.set_mode(worldtree.SCREEN_SIZE)
  fh = open(os.path.join(MAPS_PATH, 'test.map'))
  env = Environment(fh.read())
  print 'No errors reading the map!'


if __name__ == '__main__':
  # Set up dir correctly - required for compiled .exe to work reliably
  os.chdir(os.path.dirname(sys.argv[0]))
  TestMap()
