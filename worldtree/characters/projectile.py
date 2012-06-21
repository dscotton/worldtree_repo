"""
Class for projectiles.

Created on June 20, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import pygame

import animation
import character
import game_constants

class Projectile(pygame.sprite.Sprite):
  """This class is abstract, because it has no image."""
  
  IMAGE_FILE = 'defaultprojectile.png'
  IMAGE = None
  
  def __init__(self, env, damage, speed, direction, position):
    """Constructor.
    
    Args:
      env: Environment object this projectile exists in.
      damage: int amount of damage this inflicts when it hits.
      speed: int movement speed per frame.
      direction: (x, y) direction vector.  Magnitude is irrelevant, it will be rescaled.
      position: (x, y) initial screen position in pixels.
    """
    self.env = env
    self.damage = damage
    self.speed = speed
    self.direction = direction
    multiplier = speed / float((direction[0]**2 + direction[1]**2) ** 0.5)
    self.movement = [int(multiplier * direction[0]), int(multiplier * direction[1])]

  def InitImage(self):
    """Initialize the image or animation for this projectile.
    
    Override this and SetCurrentImage() to customize the image.
    """
    if Projectile.IMAGE is None:
      Projectile.IMAGE = character.LoadImage(self.IMAGE_FILE, scaled=True, 
                                             colorkey=game_constants.SPRITE_COLORKEY)
    self.image = self.IMAGE

  def SetCurrentImage(self):
    """Sets the current image for the projectile.  Only needed if it has animation."""
    pass
    
  def update(self):
    self.SetCurrentImage()
    if not self.env.IsMoveLegal(self, self.movement):
      # Only checks if this hits walls, not other sprites.
      self.kill()
    self.rect = self.rect.move(self.movement)


class SeedBullet(Projectile):
  
  DAMAGE = 2
  SPEED = 16
  IMAGES = None
  
  def __init__(self, env, direction, position):
    Projectile.__init__(self, env, self.DAMAGE, self.SPEED, direction, position)
    
  def InitImage(self):
    if SeedBullet.IMAGES is None:
      SeedBullet.IMAGES = character.LoadImages('seedprojectile*.png', scaled=True,
                                    colorkey=game_constants.SPRITE_COLORKEY)
      self.animation = animation.Animation(SeedBullet.IMAGES)

    self.image = self.animation.NextFrame()
    
  def SetCurrentImage(self):
    self.image = self.animation.NextFrame()
