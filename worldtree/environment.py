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

TILE_WIDTH = 32
TILE_HEIGHT = 32
TILE_SIZE = (TILE_WIDTH, TILE_HEIGHT)
HORIZONTAL_TILE_COUNT = worldtree.MAP_WIDTH / TILE_SIZE[0]
VERTICAL_TILE_COUNT = worldtree.MAP_HEIGHT / TILE_SIZE[1]
MAPS_PATH = os.path.join('media', 'maps')

# TODO: Add images here as the second argument.
# TODO: First create tiles outside of this map (to load the surfaces), THEN map them to
# characters here.
EMPTY_TILE = tile.Tile(tile.FULLY_EMPTY)
TILES = {
  '-' : tile.Tile(tile.FULLY_SOLID),
  ' ' : EMPTY_TILE
}

ENEMIES = {
}

class Environment(object):
  """A game environment.
  
  Attributes:
    map: A two-dimensional array of map tiles.
    surface: pygame.Surface containing the appearance of the visible part of the environment.
    screen_offset: (x, y) pixel offset of the upper-right corner of the current screen
      from the upper-right corner of the map.
  """
  
  def __init__(self, ascii_map, offset=(0,0)):
    """Constructor.
    
    Args:
      ascii_map: String representation of the area ascii_map.  Each character represents a single tile,
        with the meaning as defined above.
    """
    # This is a little convoluted because in order to address tiles as [x][y] (rather than
    # [y][x]) we need to build a list of columns rather than a list of rows.
    self.map = []
    self.screen_offset = offset
    self.surface = pygame.Surface(worldtree.MAP_SIZE)
    self._dirty = True  # Whether the surface needs to be refreshed.
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

  def VisibleTiles(self):
    """Returns the indexes of the currently visible tiles.
    
    Return tuple is ((first_column, last_column), (first_row, last_row)) for the visible area.
    """
    first_x = self.screen_offset[0] / TILE_WIDTH
    last_x = first_x + (worldtree.MAP_WIDTH / TILE_WIDTH)
    first_y = self.screen_offset[1] / TILE_HEIGHT
    last_y = first_x + (worldtree.MAP_HEIGHT / TILE_HEIGHT)
    return ((first_x, last_x), (first_y, last_y))

  def GetImage(self):
    """Get the pygame.Surface for the portion of the environment currently in the game window."""
    if self._dirty:
      # Figure out which tiles fit in the current window
      (first_x, last_x), (first_y, last_y) = self.VisibleTiles()
      x_pixel_start = self.screen_offset[0] % TILE_WIDTH
      y_pixel_start = self.screen_offset[1] % TILE_HEIGHT

      for col in range(first_x, last_x + 1):
        if col >= len(self.map):
          # Outside the right edge of the map - maybe handle this better?
          break
        for row in range(first_y, last_y + 1):
          if row >= len(self.map[col]):
            # Past the bottom of the map.
            break
          tile_x_pos = (col - first_x) * TILE_WIDTH - x_pixel_start
          tile_y_pos = (row - first_y) * TILE_HEIGHT - y_pixel_start
          self.surface.blit(self.map[col][row].image, (tile_x_pos, tile_y_pos))

      self._dirty = False
    return self.surface

def TestMap():
  """Simple test for this class - load "test.map" and validate."""
  pygame.display.set_caption("Map Test")
  screen = pygame.display.set_mode(worldtree.SCREEN_SIZE)
  fh = open(os.path.join(MAPS_PATH, 'test.map'))
  env = Environment(fh.read())
  screen.blit(env.GetImage(), worldtree.MAP_POSITION)
  pygame.display.flip()
  print 'No errors reading the map!'
  while pygame.QUIT not in (event.type for event in pygame.event.get()):
    # Show the map until exit.
    pass


if __name__ == '__main__':
  # Set up dir correctly - required for compiled .exe to work reliably
  os.chdir(os.path.dirname(sys.argv[0]))
  TestMap()
