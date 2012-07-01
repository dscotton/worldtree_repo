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
The World Tree, source of all
life...


Legend tells of a catastrophic
battle in which the evil
Beaver Baron and his legion of
minions nearly succeeded in
gnawing down the World Tree
to make a dam.


Over 1000 years have passed
since the evil Beaver Baron
was defeated by the protectors
of the World Tree.  Since that
time, the World Tree has
prospered under the care of
its guardians.


But time flows like a river...
And history repeats...


You are Seamus, one of the
World Tree's guardian spirits.
Are you a bad enough dude
to save the World Tree?
"""
CONTROLS_TEXT = """
CONTROLS


Arrow keys or WASD - move

Space bar - jump

M - attack

N - shoot (requires ammo)

Return - start
"""
JOKE_INTRO_TEXT = """
I first battled the beavers in
northern Ontario. It was there
that I foiled the plans of the
Beaver Baron to attack the
Great Lakes 'civilization'.
\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n

I next fought the beavers in
their homeland... also
northern Ontario. I completely
eradicated them except for a
larva, which after hatching
followed me like a confused
child...
\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n

I personally delivered it to
the Canadian Forestry
Commission at Canada City so
scientists could study its
tree-devouring qualities...
\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n
The scientists' findings were
astounding! They discovered
that the powers of the beaver
might be harnessed in a
delicious soup!
\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n

Satisfied that all was well,
I got the hell out of Canada
and searched for a suitable
place to set up my hammock
and sleep for at least three
months. But I had taken maybe
seven steps when I heard a
bloodcurdling shriek.
\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n

Canada was under attack!
\n\n\n\n\n\n\n

Oh well, who cares.
\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n

Two weeks later, back at
the World tree:
Goddammit where did all
these beavers come from?
It must be the beaver baron
again. Fire up the cauldron,
we're making soup tonight."""


class Title(object):
  """Class representing a screen showing some combination of image, text, and music."""
  
  def __init__(self, image=None, music=None, text=None, text_speed=6, fade_rate=4, frame_delay=0):
    """Constructor.
    
    Args:
      image: pygame.Surface containing the background image.  If none, will be a black background.
      music: str containing the filename for the background music.
      text: str containing the text to scroll.
      text_speed: int number of frames per pixel for the scrolling text. (Lower is faster.)
      fade_rate: int giving the speed at which the screen fades after the player hits enter.
      frame_delay: int number of frames before the text begins to scroll.
    """
    self.background = image
    if self.background is None:
      self.background = pygame.Surface(game_constants.SCREEN_SIZE)
      self.background.fill(game_constants.BLACK)
    self.music = music
    self.text = text
    if text is None:
      self.text = ''
    self.text_speed = text_speed
    self.fade_rate = fade_rate
    self.frame_delay = frame_delay

  def ShowTitle(self, screen):
    """Render the title screen until the player presses enter.
  
    Args:
      screen: the screen, a pygame.Surface object.
    """
  
    screen.blit(self.background, (0, 0))
    pygame.display.flip()
    clock = pygame.time.Clock()
    if self.music is not None:
      pygame.mixer.music.load(os.path.join('media', 'music', self.music))
      pygame.mixer.music.play(-1)
  
    frame = -self.frame_delay
    # Controls how long before the text scroll begins.  Mostly obsolete with text intro on
    # a separate screen.  But should be configurable to make this class more general.
    text_top = 144
    text_bottom = 576
    font_height = 16
    line_height = 20
    text_area_rect = pygame.Rect((240, text_top), (480, text_bottom - text_top))

    font = pygame.font.Font(os.path.join('media', 'font', game_constants.FONT), font_height)
    text_array = []
    lines = self.text.splitlines()
  
    while controller.START not in controller.GetInput() or (self.frame_delay - frame) > -30:
      # Frame count is there because otherwise having multiple Titles in sequence is impossible
      # because one keypress skips the second one.
      if pygame.QUIT in (event.type for event in pygame.event.get()):
        sys.exit()
      clock.tick(60)
      screen.blit(self.background, (0, 0))

      if frame >= 0:
        text_position = text_bottom - (frame / self.text_speed)
        if frame % (self.text_speed * line_height) == 0:
          line_number = frame / (self.text_speed * line_height)
          if line_number < len(lines):
            text = font.render(lines[line_number],
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
    while self.background.get_alpha() > 4:
      fade_frame +=1
      clock.tick(60)
      screen.fill(game_constants.BLACK)
      current_alpha = 255 - fade_frame * self.fade_rate
      self.background.set_alpha(current_alpha)
      screen.blit(self.background, (0, 0))
      pygame.display.flip()


def ShowTitle(screen):
  title_background = pygame.image.load(os.path.join('media', INTRO_IMAGE)).convert()
  title = Title(text=INTRO_TEXT, image=title_background, music='june_breeze.ogg', frame_delay=60)
  title.ShowTitle(screen)
  intro = Title(text=CONTROLS_TEXT, text_speed=1, fade_rate=12)
  intro.ShowTitle(screen)
