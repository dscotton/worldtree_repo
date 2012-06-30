"""
This class represents a single game tile.

Created on Jun 3, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import pygame

import game_constants

FULLY_SOLID = (True, True, True, True)
FULLY_EMPTY = (False, False, False, False)
TILE_SIZE = (32, 32)

class Tile(object):
  """A single map tile.
  
  Tiles have no notion of their position, that's handled by the Environment class.  Therefore,
  you can use a single instance for all tiles of the same type of terrain for lower memory
  consumption.
  
  Attributes:
    image: pygame.Surface object containing the tile's appearance.
    solid: 4-tuple of booleans, indicating whether the tile is solid from the left, right
      top, and bottom. (Meaning whether a sprite can enter from that direction).
    bound_byte: Tilestudio formatted boundary information for this tile.  Overrides solid if
      specified.
  """
  
  def __init__(self, image=None, solid=None, bound_byte=None, bg_color=game_constants.BLACK):
    """Constructor.
    
    Args:
      image: pygame.Surface object containing the tile's appearance.
      solid: Tuple of whether the tile is solid on the left, right, top, and bottom.
      bound_byte: Tilestudio formatted boundary information for this tile.  Overrides solid if
        specified.
      bg_color: Tuple of two-byte RGB values for the background color to put the tile over.
    """
    if solid is None and bound_byte is None:
      solid = (False, False, False, False)
    if bound_byte is not None:
      solid = ParseBoundByte(bound_byte)
    self.solid_left, self.solid_right, self.solid_top, self.solid_bottom = solid
    if image is None:
      # Can't call convert_alpha here because the screen may not have been initialized.
      image = pygame.Surface(TILE_SIZE)
      if not any(solid):
        # TODO(dscotton): If we use backgrounds, get rid of this - or make it transparent
        image.fill(bg_color)
      else:
        image.fill(game_constants.BLACK)
    self.image = image


def ParseBoundByte(bound):
  """Parse a Tile Studio format bound byte into a tuple understood by the Tile class.
  
  Args:
    bound: byte containing Tile Studio boundary flags.  The bits are 0: upper, 1: left, 2: right
      3: lower.
  Returns:
    4-tuple of booleans indicating whether the tile is bounded on the left, right, top, and bottom.
  """
  return tuple(bool(x & bound) for x in (2, 8, 1, 4))