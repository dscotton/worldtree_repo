"""
This class represents a game environment.

Created on Jun 3, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os
import sys

import pygame

from characters import enemies
from characters import powerup
from game_constants import *
import map_data
import tile

MAPS_PATH = os.path.join('media', 'maps')

# TODO: Add images here as the second argument.
EMPTY_TILE = tile.Tile()

# Map of map codes to enemy class.
ENEMIES = {
  1: enemies.Badger
}

# Map of map codes to items.  These share the same number space as the enemies, so there must
# not be any overlap.  
ITEMS = {
  129: powerup.HealthBoost
}

class Environment(object):
  """A game environment.
  
  Attributes:
    grid: A two-dimensional array of map tiles.
    surface: pygame.Surface containing the appearance of the visible part of the environment.
    screen_offset: [x, y] pixel offset of the upper-right corner of the current visible area
      from the upper-right corner of the whole map.  Must be a mutable object to support scrolling.
    height: The height of the map in number of tiles.
    width: The width of the map in number of tiles.
    dirty: boolean that's True if the map needs to be redrawn.  Is not effected by player or
      enemy movement since those aren't drawn as part of the map - only by scrolling or actually
      changing the map tiles.
    enemy_group: RendererUpdates object containing all enemy sprites.  Should be manipulated
      by the main engine and not by this class.
  """
  
  def __init__(self, map_name, offset=None):
    """Constructor.
    
    Args:
      map_name: str, name of the map in map_data dict.
      map_data: Dict containing all the layout information for a particular area. Will be
        generated by TileStudio using ts_config/worldtree.tsd.
    """
    self.name = map_name
    map_info = map_data.map_data[map_name]
    # This is a little convoluted because in order to address tiles as [x][y] (rather than
    # [y][x]) we need to build a list of columns rather than a list of rows.
    self.grid = []
    self.height = map_info['height']
    self.width = map_info['width']
    if offset is None:
      self.screen_offset = [0, 0]
    else:
      self.screen_offset = list(offset)
    self.surface = pygame.Surface(MAP_SIZE)
    self.dirty = True  # Whether the surface needs to be refreshed.
    self.enemy_group = pygame.sprite.RenderUpdates()
    self.item_group = pygame.sprite.RenderUpdates()
    image_cache = {}  # Only create one Surface for each image.
    for row in range(self.height):
      for col in range(self.width):
        if row == 0:
          # Extend the grid to width = col.
          self.grid.append([])
        if map_info['layout'][row][col] == 0:
          self.grid[col].append(EMPTY_TILE)
          continue
        
        image_name = '%s-%s.png' % (map_info['tileset'], map_info['layout'][row][col])
        if image_name not in image_cache:
          image_path = os.path.join(TILE_DIR, image_name)
          image = pygame.transform.scale(pygame.image.load(image_path), TILE_SIZE).convert_alpha()
          image_cache[image_name] = image
        self.grid[col].append(tile.Tile(image=image_cache[image_name],
                                        bound_byte=map_info['bounds'][row][col]))
        mapcode = map_info['mapcodes'][row][col]
        if mapcode != 0:
          if mapcode in ENEMIES:
            self.enemy_group.add(ENEMIES[mapcode](self, (col, row)))
          elif mapcode in ITEMS:
            self.item_group.add(ITEMS[mapcode](self, (col, row)))
          else:
            raise Exception("Unknown mapcode: %s" % mapcode)
    # TODO: Prevent enemies from walking into items.

  def VisibleTiles(self):
    """Returns the indexes of the currently visible tiles.

    Return tuple is ((first_column, last_column), (first_row, last_row)) for the visible area.
    """
    first_x = self.screen_offset[0] / TILE_WIDTH
    last_x = first_x + (MAP_WIDTH / TILE_WIDTH)
    first_y = self.screen_offset[1] / TILE_HEIGHT
    last_y = first_y + (MAP_HEIGHT / TILE_HEIGHT)
    return ((first_x, last_x), (first_y, last_y))

  def GetImage(self):
    """Get the pygame.Surface for the portion of the environment currently in the game window."""
    if self.dirty:
      self.surface.fill(BG_COLOR)
      # Figure out which tiles fit in the current window
      (first_x, last_x), (first_y, last_y) = self.VisibleTiles()
      x_pixel_start = self.screen_offset[0] % TILE_WIDTH
      y_pixel_start = self.screen_offset[1] % TILE_HEIGHT

      for col in range(first_x, last_x + 1):
        if col >= len(self.grid):
          # Outside the right edge of the grid - maybe handle this better?
          break
        for row in range(first_y, last_y + 1):
          if row >= len(self.grid[col]):
            # Past the bottom of the grid.
            break
          tile_x_pos = (col - first_x) * TILE_WIDTH - x_pixel_start
          tile_y_pos = (row - first_y) * TILE_HEIGHT - y_pixel_start
          try:
            self.surface.blit(self.grid[col][row].image, (tile_x_pos, tile_y_pos))
          except TypeError:
            print col, row, type(self.grid[col][row])

      self.dirty = False
    return self.surface

  def AttemptMove(self, sprite, vector):
    """Checks whether a sprite's attempted movement is legal.

    Args:
      sprite: pygame.sprite.Sprite that's trying to move.
      vector: the x, y motion vector the sprite is trying to move along.
    Returns:
      A Rect for the position the sprite ends up in based on its motion and interaction with
      the environment.
    """
    # Hack to work around the environment not beginning at the top of the screen
    hitbox = sprite.Hitbox()
    dest = hitbox.move(vector)
    new_vector = list(vector)
    # Figure out which tiles contain the old and new position.
    old_tiles = self.TilesForRect(hitbox)
    new_tiles = [t for t in self.TilesForRect(dest) if t not in old_tiles]
    # For each new tile the sprite would occupy, check whether it blocks movement in the
    # desired direction.
    for col, row in new_tiles:
      tile_rect = self.RectForTile(col, row)
      # Stop non-player sprites from moving outside the room.  Allow players to move this way,
      # And check for transitions in the main loop.
      if col < 0 or col >= self.width or row < 0 or row >= self.height:
        if sprite.IS_PLAYER:
          square = tile.Tile(solid=(False, False, False, False))
        else:
          square = tile.Tile(solid=(True, True, True, True))
      else:
        square = self.grid[col][row]

      # Handle motion in each cardinal direction separately.  Need to check three conditions:
      # that sprite was previously on a particular side of the tile, and that entry from that
      # side is forbidden, and that the movement in this direction hasn't already been stopped
      # short.
      if hitbox.bottom < tile_rect.top and square.solid_top and dest.bottom >= tile_rect.top:
        new_vector[1] = tile_rect.top - hitbox.bottom - 1
      elif (hitbox.top > tile_rect.bottom and square.solid_bottom
            and dest.top <= tile_rect.bottom):
        new_vector[1] = tile_rect.bottom - hitbox.top + 1
      if hitbox.right < tile_rect.left and square.solid_left and dest.right >= tile_rect.left:
        new_vector[0] = tile_rect.left - hitbox.right - 1
      elif (hitbox.left > tile_rect.right and square.solid_right
            and dest.left <= tile_rect.right):
        new_vector[0] = tile_rect.right - hitbox.left + 1

    new_position = sprite.rect.move(new_vector)
    return new_position
    
  def TilesForRect(self, rect):
    """Returns a set of tiles that a rect falls in.

    The rect is a position on the map, including any portions currently offscreen.
    """
    left_col = rect.left / TILE_WIDTH
    right_col = rect.right / TILE_WIDTH
    top_row = rect.top / TILE_HEIGHT
    bottom_row = rect.bottom / TILE_HEIGHT
    return [(col, row) for col in range (left_col, right_col+1)
            for row in range(top_row, bottom_row+1)]

  def RectForTile(self, col, row):
    """Return a Rect object for a particular tile, from its map column and row.
    
    This rect is relative to the origin of the map, not the screen.
    """
    left = col * TILE_WIDTH
    top = row * TILE_HEIGHT
    return pygame.Rect(left, top, TILE_WIDTH-1, TILE_HEIGHT-1)

  def TileIndexForPoint(self, x, y):
    """Return the col, row index for the tile containing map coordinate (x, y)."""
    return (x / TILE_WIDTH, y / TILE_HEIGHT)

  def ScreenCoordinateForMapPoint(self, x, y):
    """Convert map-relative (x, y) pixel coordinates into screen-relative coordinates."""
    return (x + MAP_X - self.screen_offset[0], y + MAP_Y - self.screen_offset[1])
  
  def MapCoordinateForScreenPoint(self, x, y):
    """Convert screen-relative (x, y) point into a map-relative coordinate."""
    return (x - MAP_X + self.screen_offset[0], y - MAP_Y + self.screen_offset[1])

  def IsOutsideMap(self, rect):
    """True if the center of rect has moved outside the map."""
    col, row = self.TileIndexForPoint(rect.centerx, rect.centery)
    if col < 0 or col >= self.width or row < 0 or row >= self.height:
      return True
    return False

  def IsRectSupported(self, rect):
    """Returns true if there is a solid tile directly under a rectangle.
    
    Args:
      rect: A Rect object whose position is relative to the map origin (not the screen origin).
    """
    dest = rect.move((0, 1))
    old_tiles = self.TilesForRect(rect)
    new_tiles = [tile for tile in self.TilesForRect(dest) if tile not in old_tiles]
    for col, row in new_tiles:
      if col < 0 or col >= self.width:
        continue
      if row >= self.height:
        return False
      try:
        if self.grid[col][row].solid_top:
          return True
      except IndexError:
        print col, row
        raise
    return False

  def IsTileSupported(self, col, row):
    """Returns true if there is a solid tile directly under the tile being checked."""
    return self.IsRectSupported(self.RectForTile(col, row))

  def Scroll(self, rect):
    """If necessary, scroll the map to follow the position of rect.
    
    Returns:
      (x, y) vector to apply to rect to maintain its relative position with the landscape.
    """
    # x_scroll and y_scroll are calculated with opposite signs (meaning subtracted from the
    # current screen offset) in order to correctly move characters around the map to maintain
    # relative position.
    x_scroll = 0
    y_scroll = 0
    if rect.centerx < SCROLL_MARGIN and self.screen_offset[0] > 0:
      x_scroll = min(SCROLL_MARGIN - rect.centerx, self.screen_offset[0])
      self.screen_offset[0] = self.screen_offset[0] - x_scroll
      self.dirty = True
    elif (rect.centerx > MAP_WIDTH - SCROLL_MARGIN
          and self.screen_offset[0] + MAP_WIDTH < self.width * TILE_WIDTH):
      x_scroll = max(MAP_WIDTH - SCROLL_MARGIN - rect.centerx,
                     self.screen_offset[0] - self.width * TILE_WIDTH - MAP_WIDTH)
      self.screen_offset[0] = self.screen_offset[0] - x_scroll
      self.dirty = True
      
    if rect.centery < SCROLL_MARGIN + MAP_Y and self.screen_offset[1] > 0:
      y_scroll = min(SCROLL_MARGIN + MAP_Y - rect.centery, self.screen_offset[1])
      self.screen_offset[1] = self.screen_offset[1] - y_scroll
      self.dirty = True
    elif (rect.centery > MAP_HEIGHT + MAP_Y - SCROLL_MARGIN
          and self.screen_offset[1] + MAP_HEIGHT < self.height * TILE_HEIGHT):
      y_scroll = max(MAP_HEIGHT + MAP_Y - SCROLL_MARGIN - rect.centery,
                     self.screen_offset[1] + MAP_Y - self.height * TILE_HEIGHT - MAP_HEIGHT)
      self.screen_offset[1] = self.screen_offset[1] - y_scroll
      self.dirty = True

    scroll_vector = (x_scroll, y_scroll)
    # Must move all enemies to account for the shifted window.
    for enemy in self.enemy_group:
      enemy.rect = enemy.rect.move(scroll_vector)
    for item in self.item_group:
      item.rect = item.rect.move(scroll_vector)
    return scroll_vector
