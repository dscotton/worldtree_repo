"""
This class represents a single game tile.

Created on Jun 3, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import pygame

import worldtree

TILE_SIZE = (32, 32)

class Tile(object):
  """A single map tile.
  
  Tiles have no notion of their position, that's handled by the Environment class.  Therefore,
  you can use a single instance for all tiles of the same type of terrain for lower memory
  consumption.
  
  Attributes:
    solid: Boolean indicating whether sprites can pass through this tile.
    image: pygame.Surface object containing the tile's appearance.
  """
  
  def __init__(self, solid=False, image=None):
    self.solid = solid
    if image is None:
      # Can't call convert_alpha here because the screen may not have been initialized.
      image = pygame.Surface(TILE_SIZE)
      if not solid:
        # TODO(dscotton): If we use backgrounds, get rid of this - or make it transparent
        image.fill(worldtree.BG_COLOR)
      else:
        image.fill(worldtree.BLACK)
