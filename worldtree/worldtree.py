"""
Main executable for the World Tree game.

Written for NaGaDeMo 2012 - http://nagademo.com/

Created on June 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import sys

import pygame
pygame.mixer.pre_init(44100, -16, 2, 2048)
pygame.init()

from characters import character
from characters import hero
from characters import powerup
import controller
import environment
from game_constants import *
import map_data
import map_transitions
import statusbar
import titlescreen

def RunGame():
  pygame.display.set_caption(GAME_NAME)
  screen = pygame.display.set_mode(SCREEN_SIZE)
  screen.fill(BLACK)
#  titlescreen.ShowTitle(screen)
  
  clock = pygame.time.Clock()

  current_room = 'Map31'
  current_region = 2
  env = environment.Environment(current_room, current_region)
  screen.blit(env.GetImage(), MAP_POSITION)
  player = hero.Hero(env, position=(51, 10))
#  player = hero.Hero(env, position=(3, 10))
  player_group = pygame.sprite.RenderUpdates(player)
  enemy_group = env.enemy_group
  item_group = env.item_group
  item_group.draw(screen)
  player_group.draw(screen)
  enemy_group.draw(screen)

  status = statusbar.Statusbar(player)
  screen.blit(status.GetImage(), (0, 0))
  pygame.display.flip()

  current_song = None
  if current_room in environment.SONGS_BY_ROOM[current_region]:
    current_song = environment.SONGS_BY_ROOM[current_region][current_room]
    pygame.mixer.music.load(os.path.join('media', 'music', current_song))
    pygame.mixer.music.play(-1)
  while pygame.QUIT not in (event.type for event in pygame.event.get()):
    clock.tick(60)
    screen.fill(BLACK)
    dirty_rects = []

    collisions = pygame.sprite.spritecollide(player, enemy_group, False, 
                                             collided=character.CollideCharacters)
    for enemy in collisions:
      player.CollideWith(enemy)
    item_pickups = pygame.sprite.spritecollide(player, item_group, False, 
                                               collided=powerup.CollideSprites)
    for item in item_pickups:
      item.PickUp(player)
    for bullet in env.hero_projectile_group:
      hit_enemies = pygame.sprite.spritecollide(bullet, enemy_group, False)
      for enemy in hit_enemies:
        bullet.CollideWith(enemy)
        bullet.kill()
    bullets = pygame.sprite.spritecollide(player, env.enemy_projectile_group, False)
    for bullet in bullets:
      bullet.CollideWith(player)
      bullet.kill()
    player.HandleInput()
    player_group.update()
    enemy_group.update()
    item_group.update()
    try:
      env.dying_animation_group.update()
    except GameWonException:
      print 'You won!'
      titlescreen.ShowCredits(screen)
      raise GameOverException()
    env.hero_projectile_group.update()
    env.enemy_projectile_group.update()
    refresh_map = env.dirty
    screen.blit(env.GetImage(), MAP_POSITION)
    dirty_rects = player_group.draw(screen)
    dirty_rects.extend(item_group.draw(screen))
    dirty_rects.extend(enemy_group.draw(screen))
    dirty_rects.extend(env.hero_projectile_group.draw(screen))
    dirty_rects.extend(env.enemy_projectile_group.draw(screen))
    dirty_rects.extend(env.dying_animation_group.draw(screen))
    if refresh_map:
      dirty_rects = [pygame.Rect(MAP_POSITION[0], MAP_POSITION[1], MAP_WIDTH, MAP_HEIGHT)]
    else:
      for rect in dirty_rects:
        # For some reason the returned dirty_rects doesn't draw the entire sprite for the
        # main character when moving.
        rect.top -= 3
        rect.left -= 3
        rect.width += 6
        rect.height += 6
    screen.blit(status.GetImage(), (0, 0))
    if status.dirty:
      dirty_rects.append(pygame.Rect(0, 0, SCREEN_WIDTH, MAP_Y))
      status.dirty = False
    if len(player_group) == 0:
      # Player is dead
      font = pygame.font.Font(os.path.join('media', 'font', FONT), 24)
      game_over_text = font.render('Game Over', False, WHITE)
      game_over_text_box = game_over_text.get_rect()
      game_over_text_box.centerx = SCREEN_WIDTH / 2
      game_over_text_box.centery = SCREEN_HEIGHT / 2
      screen.blit(game_over_text, game_over_text_box)
      dirty_rects.append(game_over_text_box)
      
    pygame.display.update(dirty_rects)
      
    # Check if character is leaving the area and make the transition.
    if env.IsOutsideMap(player.Hitbox()):
      # Use the character's center to determine when they leave the map, but for all other
      # positioning use their upper left corner for precision.
      tile_x, tile_y = env.TileIndexForPoint(player.Hitbox().centerx, player.Hitbox().centery)
      ul_x, ul_y = env.TileIndexForPoint(player.Hitbox().left, player.Hitbox().top)
      new_room = None
      new_region = None
      if tile_x < 0:
        for trans in map_transitions.transitions[current_region][current_room].get(LEFT, []):
          if ul_y >= trans.first and ul_y <= trans.last:
            new_room = trans.dest
            new_region = trans.region
            new_map = environment.REGIONS[new_region][new_room]
            x_pos = new_map['width'] - 1
            y_pos = ul_y + trans.offset
            screen_offset_x = new_map['width'] * TILE_WIDTH - MAP_WIDTH
            screen_offset_y = min(new_map['height'] * TILE_HEIGHT - MAP_HEIGHT,
                                  max(env.screen_offset[1] + trans.offset * TILE_HEIGHT, 0))
      elif tile_x >= env.width:
        for trans in map_transitions.transitions[current_region][current_room].get(RIGHT, []):
          if ul_y >= trans.first and ul_y <= trans.last:
            new_room = trans.dest
            new_region = trans.region
            new_map = environment.REGIONS[new_region][new_room]
            x_pos = 0
            y_pos = ul_y + trans.offset
            screen_offset_x = 0
            screen_offset_y = min(new_map['height'] * TILE_HEIGHT - MAP_HEIGHT,
                                  max(env.screen_offset[1] + trans.offset * TILE_HEIGHT, 0))
      elif tile_y < 0:
        for trans in map_transitions.transitions[current_region][current_room].get(UP, []):
          if ul_x >= trans.first and ul_x <= trans.last:
            new_room = trans.dest
            new_region = trans.region
            new_map = environment.REGIONS[new_region][new_room]
            x_pos = ul_x + trans.offset
            y_pos = new_map['height'] - 1
            screen_offset_x = min(new_map['width'] * TILE_WIDTH - MAP_WIDTH,
                                  max(env.screen_offset[0] + trans.offset * TILE_WIDTH, 0))
            screen_offset_y = new_map['height'] * TILE_HEIGHT - MAP_HEIGHT
      elif tile_y >= env.height:
        for trans in map_transitions.transitions[current_region][current_room].get(DOWN, []):
          if ul_x >= trans.first and ul_x <= trans.last:
            new_room = trans.dest
            new_region = trans.region
            new_map = environment.REGIONS[new_region][new_room]
            x_pos = ul_x + trans.offset
            y_pos = 0
            screen_offset_x = min(new_map['width'] * TILE_WIDTH - MAP_WIDTH,
                                  max(env.screen_offset[0] + trans.offset * TILE_WIDTH, 0))
            screen_offset_y = 0
            
      if new_room is not None:
        current_region = new_region
        current_room = new_room  
        env = environment.Environment(current_room, current_region,
                                      offset=(screen_offset_x, screen_offset_y))
        player.ChangeRooms(env, (x_pos, y_pos))
        if current_room in environment.SONGS_BY_ROOM[current_region]:
          new_song = environment.SONGS_BY_ROOM[current_region][current_room]
          if new_song != current_song:
            current_song = new_song
            pygame.mixer.music.fadeout(250)
            pygame.mixer.music.load(os.path.join('media', 'music', current_song))
            pygame.mixer.music.play(-1)
        else:
          pygame.mixer.music.stop()
        enemy_group = env.enemy_group
        item_group = env.item_group
    
  sys.exit()

if __name__ == '__main__':
  # Set up dir correctly - required for compiled .exe to work reliably
  os.chdir(os.path.dirname(sys.argv[0]))
  while True:
    try:
      RunGame()
    except GameOverException:
      pass
