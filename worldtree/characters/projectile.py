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
    pygame.sprite.Sprite.__init__(self)
    self.env = env
    self.damage = damage
    self.speed = speed
    self.direction = direction
    multiplier = speed / float((direction[0]**2 + direction[1]**2) ** 0.5)
    self.movement = [int(multiplier * direction[0]), int(multiplier * direction[1])]
    self.InitImage()
    self.rect = pygame.Rect(position, self.image.get_size())

  def Hitbox(self):
    """Gets the Map hitbox for the sprite, which is relative to the map rather than the screen.
    
    The Hitbox needs to be smaller than the sprite, partly because of weird PyGame behavior
    where a rect of width X and height y actually touches (x+1) * (y+1) pixels.
    """
    x, y = self.env.MapCoordinateForScreenPoint(self.rect.left, self.rect.top)
    return pygame.Rect(x, y, self.rect.width, self.rect.height)

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

  def CollideWith(self, sprite):
    """Handle what happens when the projectile hits another sprite.
    
    Override this if you want to do more than just damage (e.g. apply pushback)
    """
    if not sprite.invulnerable:
      sprite.TakeHit(self.damage)
    
  def update(self):
    self.SetCurrentImage()
    if not self.env.IsMoveLegal(self, self.movement):
      # Only checks if this hits walls, not other sprites.
      self.kill()
    self.rect = self.rect.move(self.movement)


class SeedBullet(Projectile):
  
  DAMAGE = 2
  SPEED = 12
  IMAGES = None
  
  def __init__(self, env, direction, position):
    Projectile.__init__(self, env, self.DAMAGE, self.SPEED, direction, position)
    
  def InitImage(self):
    if SeedBullet.IMAGES is None:
      SeedBullet.IMAGES = character.LoadImages('seedprojectile*.png', scaled=True,
                                               colorkey=game_constants.SPRITE_COLORKEY)
    self.animation = animation.Animation(SeedBullet.IMAGES, framedelay=5)
    self.image = self.animation.NextFrame()
    
  def SetCurrentImage(self):
    self.image = self.animation.NextFrame()


class SporeCloud(Projectile):
  
  DAMAGE = 2
  SPEED = 3
  IMAGE = None

  def __init__(self, env, direction, position):
    Projectile.__init__(self, env, self.DAMAGE, self.SPEED, direction, position)
    # Adjust position away from the center of the shooter.
    print self.movement
    self.rect.left += (self.movement[0] * 5)
    self.rect.top += (self.movement[1] * 15)
    
  def InitImage(self):
    if SporeCloud.IMAGE is None:
      SporeCloud.IMAGE = character.LoadImage('spore0000.png', scaled=True,
                                             colorkey=game_constants.SPRITE_COLORKEY)
    self.image = self.IMAGE
    
