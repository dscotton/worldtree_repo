"""
Main executable for the World Tree game.

Written for NaGaDeMo 2012 - http://nagademo.com/

Created on June 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os
import sys

import pygame
pygame.init()

from characters import hero
import environment

GAME_NAME = 'World Tree'
SCREEN_WIDTH = 960
SCREEN_HEIGHT = 720
MAP_WIDTH = 960
MAP_HEIGHT = 640
MAP_POSITION = (0, SCREEN_HEIGHT - MAP_HEIGHT)
SCREEN_SIZE = (SCREEN_WIDTH, SCREEN_HEIGHT)
MAP_SIZE = (SCREEN_WIDTH, MAP_HEIGHT)
BLACK = (0, 0, 0)
WHITE = (0xFF, 0xFF, 0xFF)
BG_COLOR = (0xAA, 0xAA, 0xAA)

def RunGame():
  # root_path = os.getcwd()
  # media_path = os.path.join(root_path, 'media')
  # image_path = os.path.join(media_path, 'sprites')
  pygame.display.set_caption(GAME_NAME)
  screen = pygame.display.set_mode(SCREEN_SIZE)
  screen.fill(BG_COLOR)
  clock = pygame.time.Clock()

  fh = open(os.path.join(environment.MAPS_PATH, 'test.map'))
  env = environment.Environment(fh.read())
  screen.blit(env.GetImage(), MAP_POSITION)
  # TODO: System for figuring out initial position on the map.
  player = hero.Hero(env, position=(64, SCREEN_HEIGHT - MAP_HEIGHT))
  player_group = pygame.sprite.RenderUpdates(player)
  
  pygame.display.flip()
  # pygame.mixer.music.load(os.path.join(os.getcwd(), 'media', 'theme.ogg'))
  # pygame.mixer.music.play()
  while pygame.QUIT not in (event.type for event in pygame.event.get()):
    clock.tick(60)
    screen.fill(BG_COLOR)
    screen.blit(env.GetImage(), MAP_POSITION)
    dirty_rects = []
    
    # Input test
    '''
    font = pygame.font.Font(os.path.join('media', 'font', 'PressStart2P.ttf'), 36)
    status_text = ', '.join(str(x) for x in controller.GetInput())
    text = font.render(status_text, False, BLACK)
    text_box = text.get_rect()
    text_box.centerx = screen.get_rect().centerx
    text_box.centery = screen.get_rect().centery
    screen.blit(text, text_box)
    '''
    player.HandleInput()
    player_group.update()
    # TODO(dscotton): draw the background on the screen instead.
    dirty_rects = player_group.draw(screen)
    pygame.display.update(dirty_rects)
    
  sys.exit()

if __name__ == '__main__':
  # Set up dir correctly - required for compiled .exe to work reliably
  os.chdir(os.path.dirname(sys.argv[0]))
  RunGame()