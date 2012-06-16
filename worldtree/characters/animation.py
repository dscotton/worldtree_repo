"""
Class encapsulating an animation loop.

Created on Jun 13, 2012

@author: dscotton@gmail.com (David Scotton)
"""

class Animation(object):
  """This class contains all the elements of an animation.
  
  Use the NextFrame() method to get the next image in the animation.
  """
  
  def __init__(self, images, framedelay=2, looping=True):
    self.current = 0
    self.framedelay = framedelay
    self.framecount = 0
    self.looping = looping
    self.images = images
    
  def NextFrame(self):
    image = self.images[self.current]
    self.framecount += 1
    if self.framecount == self.framedelay:
      self.current += 1
      self.framecount = 0
      if self.current >= len(self.images):
        if self.looping:
          self.current = 0
        else:
          self.current -= 1
    return image
  
  def Reset(self):
    """Reset the animation back to the first frame.  Only needed for non-looping animations."""
    self.current = 0
    self.framecount = 0