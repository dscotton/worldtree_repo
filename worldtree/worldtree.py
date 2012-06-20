"""
Main executable for the World Tree game.

Written for NaGaDeMo 2012 - http://nagademo.com/

Created on June 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import sys

import pygame
pygame.init()

from characters import hero
from characters import character
import environment
from game_constants import *
import map_data
import map_transitions

def RunGame():
  pygame.display.set_caption(GAME_NAME)
  screen = pygame.display.set_mode(SCREEN_SIZE)
  screen.fill(BLACK)
  clock = pygame.time.Clock()

  current_room = 'Map1'
  env = environment.Environment(current_room)
  font = pygame.font.Font(os.path.join('media', 'font', 'PressStart2P.ttf'), 24)
  # TODO: Make the status bar a class
  text = font.render("Level 1", False, WHITE)
  text_box = text.get_rect()
  text_box.top = 10
  text_box.left = 10
  screen.blit(text, text_box)

  screen.blit(env.GetImage(), MAP_POSITION)
  player = hero.Hero(env, position=(1, 2))
  player_group = pygame.sprite.RenderUpdates(player)
  enemy_group = env.enemy_group
  item_group = env.item_group
  item_group.draw(screen)
  player_group.draw(screen)
  enemy_group.draw(screen)
  
  pygame.display.flip()
  pygame.mixer.music.load(os.path.join('media', 'music', 'photosynthesis_wip.ogg'))
  pygame.mixer.music.play(-1)
  while pygame.QUIT not in (event.type for event in pygame.event.get()):
    clock.tick(60)
    screen.fill(BLACK)
    dirty_rects = []

    player.HandleInput()
    player_group.update()
    item_group.update()
    enemy_group.update()
    # TODO: Write a custom collided method, to use hitboxes if nothing else
    collisions = pygame.sprite.spritecollide(player, enemy_group, False, 
                                             collided=character.CollideCharacters)
    for enemy in collisions:
      player.CollideWith(enemy)
    # TODO: Powerup hitbox is too big
    item_pickups = pygame.sprite.spritecollide(player, item_group, False, collided=None)
    for item in item_pickups:
      item.PickUp(player)
    refresh_map = env.dirty
    screen.blit(env.GetImage(), MAP_POSITION)
    dirty_rects.extend(item_group.draw(screen))  # Not necessary to draw every frame unless animated
    dirty_rects = player_group.draw(screen)
    dirty_rects.extend(enemy_group.draw(screen))
    if refresh_map:
      pygame.display.update(pygame.Rect(MAP_POSITION[0], MAP_POSITION[1], MAP_WIDTH, MAP_HEIGHT))
    else:
      for rect in dirty_rects:
        # For some reason the returned dirty_rects doesn't draw the entire sprite for the
        # main character when moving.
        rect.top -= 3
        rect.left -= 3
        rect.width += 6
        rect.height += 6
      pygame.display.update(dirty_rects)
      
    # Check if character is leaving the area and make the transition.
    if env.IsOutsideMap(player.Hitbox()):
      # Use the character's center to determine when they leave the map, but for all other
      # positioning use their upper left corner for precision.
      tile_x, tile_y = env.TileIndexForPoint(player.Hitbox().centerx, player.Hitbox().centery)
      ul_x, ul_y = env.TileIndexForPoint(player.Hitbox().left, player.Hitbox().top)
      if tile_x < 0:
        for trans in map_transitions.transitions[current_room][LEFT]:
          if ul_y >= trans.first and ul_y <= trans.last:
            current_room = trans.dest
            new_map = map_data.map_data[current_room]
            x_pos = new_map['width'] - 1
            y_pos = ul_y + trans.offset
            screen_offset_x = new_map['width'] * TILE_WIDTH - MAP_WIDTH
            screen_offset_y = min(new_map['height'] * TILE_HEIGHT - MAP_HEIGHT,
                                  max(env.screen_offset[1] + trans.offset * TILE_HEIGHT, 0))
      elif tile_x >= env.width:
        for trans in map_transitions.transitions[current_room][RIGHT]:
          if ul_y >= trans.first and ul_y <= trans.last:
            current_room = trans.dest
            new_map = map_data.map_data[current_room]
            x_pos = 0
            y_pos = ul_y + trans.offset
            screen_offset_x = 0
            screen_offset_y = min(new_map['height'] * TILE_HEIGHT - MAP_HEIGHT,
                                  max(env.screen_offset[1] + trans.offset * TILE_HEIGHT, 0))
      elif tile_y < 0:
        for trans in map_transitions.transitions[current_room][UP]:
          if ul_x >= trans.first and ul_x <= trans.last:
            current_room = trans.dest
            new_map = map_data.map_data[current_room]
            x_pos = ul_x + trans.offset
            y_pos = new_map['height'] - 1
            screen_offset_x = min(new_map['width'] * TILE_WIDTH - MAP_WIDTH,
                                  max(env.screen_offset[0] + trans.offset * TILE_WIDTH, 0))
            screen_offset_y = new_map['height'] * TILE_HEIGHT - MAP_HEIGHT
      elif tile_y >= env.height:
        for trans in map_transitions.transitions[current_room][DOWN]:
          if ul_x >= trans.first and ul_x <= trans.last:
            current_room = trans.dest
            new_map = map_data.map_data[current_room]
            x_pos = ul_x + trans.offset
            y_pos = 0
            screen_offset_x = min(new_map['width'] * TILE_WIDTH - MAP_WIDTH,
                                  max(env.screen_offset[0] + trans.offset * TILE_WIDTH, 0))
            screen_offset_y = 0
      env = environment.Environment(current_room, offset=(screen_offset_x, screen_offset_y))
      player.ChangeRooms(env, (x_pos, y_pos))
      enemy_group = env.enemy_group
      item_group = env.item_group
    
  sys.exit()

if __name__ == '__main__':
  # Set up dir correctly - required for compiled .exe to work reliably
  os.chdir(os.path.dirname(sys.argv[0]))
  RunGame()