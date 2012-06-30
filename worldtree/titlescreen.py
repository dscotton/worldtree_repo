"""
Title screen for the Worldtree game.

Created on June 25, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os
import sys

import pygame

import controller
import game_constants

INTRO_IMAGE = 'titlescreen.png'
INTRO_TEXT = """
Arma virumque cano, 
Troiae qui primus ab oris
Italiam, fato profugus, 
Laviniaque venit
litora, multum ille
et terris iactatus et alto
vi superum saevae memorem
Iunonis ob iram;
multa quoque et bello passus,
dum conderet urbem,
inferretque deos Latio, 
genus unde Latinum,
Albanique patres,
atque altae moenia Romae.

Musa, mihi causas memora, 
quo numine laeso,
quidve dolens,
regina deum tot volvere casus
insignem pietate virum,
tot adire labores impulerit.
Tantaene animis caelestibus irae?
"""

def ShowTitle(screen):
  """Render the title screen until the player presses enter.
  
  Args:
    screen: the screen, a pygame.Surface object.
  """
  
  background = pygame.image.load(os.path.join('media', INTRO_IMAGE)).convert()
  screen.blit(background, (0, 0))
  pygame.display.flip()
  clock = pygame.time.Clock()
  pygame.mixer.music.load(os.path.join('media', 'music', 'june_breeze.ogg'))
  pygame.mixer.music.play(-1)
  
  frame = -240  # Controls how long before the text scroll begins
  text_top = 144
  text_bottom = 576
  move_rate = 5
  font_height = 16
  line_height = 20
  text_area_rect = pygame.Rect((240, text_top), (480, text_bottom - text_top))

  font = pygame.font.Font(os.path.join('media', 'font', game_constants.FONT), font_height)
  text_array = []
  
  while controller.START not in controller.GetInput():
    if pygame.QUIT in (event.type for event in pygame.event.get()):
      sys.exit()
    clock.tick(60)
    screen.blit(background, (0, 0))

    if frame >= 0:
      text_position = text_bottom - (frame / move_rate)
      if frame % (move_rate * line_height) == 0:
        line_number = frame / (move_rate * line_height)
        if line_number < len(INTRO_TEXT.splitlines()):
          text = font.render(INTRO_TEXT.splitlines()[line_number],
                             False, game_constants.WHITE)
          text_array.append(text)
    for i in range(len(text_array)):
      text = text_array[i]
      text_box = text.get_rect()
      text_box.top = text_position + i * line_height
      text_box.centerx = 480
      screen.blit(text, text_box)

    frame += 1
    pygame.display.update(text_area_rect)

  pygame.mixer.music.stop()
  fade_frame = 0
  fade_rate = 3
  while background.get_alpha() > 4:
    fade_frame +=1
    clock.tick(60)
    screen.fill(game_constants.BLACK)
    current_alpha = 255 - fade_frame * fade_rate
    background.set_alpha(current_alpha)
    screen.blit(background, (0, 0))
    # TODO: Figure out a way to make the text fade out but not appear on the upper part of
    # the screen.
    '''
    for i in range(len(text_array)):
      text = text_array[i]
      text.set_alpha(current_alpha)
      text_box = text.get_rect()
      text_box.top = text_position + i * line_height
      text_box.centerx = 480
      screen.blit(text, text_box)
    '''
    pygame.display.flip()
