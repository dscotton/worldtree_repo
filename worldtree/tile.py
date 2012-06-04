"""
This class represents a single game tile.

Created on Jun 3, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import pygame

import worldtree

FULLY_SOLID = (True, True, True, True)
FULLY_EMPTY = (False, False, False, False)
TILE_SIZE = (32, 32)

class Tile(object):
  """A single map tile.
  
  Tiles have no notion of their position, that's handled by the Environment class.  Therefore,
  you can use a single instance for all tiles of the same type of terrain for lower memory
  consumption.
  
  Attributes:
    solid_left: Boolean indicating whether sprites can enter this tile from the right.
    solid_right: Boolean indicating whether sprites can enter this tile from the left.
    solid_top: Boolean indicating whether sprites can enter this tile from the above.
    solid_bottom: Boolean indicating whether sprites can enter this tile from the below.
    image: pygame.Surface object containing the tile's appearance.
  """
  
  def __init__(self, solid=(False, False, False, False), image=None):
    """Constructor.
    
    Args:
      solid: Tuple of whether the tile is solid on the left, right, top, and bottom.
      image: pygame.Surface object containing the tile's appearance.
    """
    self.solid_left, self.solid_right, self.solid_top, self.solid_bottom = solid
    if image is None:
      # Can't call convert_alpha here because the screen may not have been initialized.
      image = pygame.Surface(TILE_SIZE)
      if not any(solid):
        # TODO(dscotton): If we use backgrounds, get rid of this - or make it transparent
        image.fill(worldtree.BG_COLOR)
      else:
        image.fill(worldtree.BLACK)
    self.image = image
