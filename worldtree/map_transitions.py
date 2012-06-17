"""
Data about transitions from the edge of one room to another.

Created on Jun 16, 2012

@author: dscotton@gmail.com (David Scotton)
"""

from controller import LEFT
from controller import RIGHT
from controller import UP
from controller import DOWN

class Transition(object):
  """Encapsulates the data describing a one-way transition between maps.
  
  All transitions take place along a single axis (i.e. left-right or up-down).  For simplicity
  the fields in this object are all relative to that axis, thus, first is the first x tile for
  an up-down transition, but the first y tile for a left-right transition.
  
  Attributes:
    direction: constant from controller, one of UP, DOWN, LEFT, RIGHT, the player is moving.
    first: int, tile index where this edge is first connected to another map.
    last: int, tile index where the edge is last connected to another map (inclusive).
    dest: str, the name of the destination room (one of the keys to map_data).
    offset: int, number of tiles offset the destination map is.  The player will appear at the
      tile given by current_position + offset.
  """

  def __init__(self, direction, first, last, destination, offset):
    self.direction = direction
    self.first = first
    self.last = last
    self.dest = destination
    self.offset = offset


transitions = {
  'Map1' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map4', 0), 
            Transition(LEFT, 20, 39, 'Map2', -20)),
    DOWN : (Transition(DOWN, 15, 49, 'Map3', -15),),
    RIGHT : (Transition(RIGHT, 0, 17, 'Map5', 17),)
  },
  'Map2' : {
    RIGHT : (Transition(RIGHT, 0, 19, 'Map1', 20),),
    UP : (Transition(UP, 0, 79, 'Map4', 0),),
  },
  'Map3' : {
    UP : (Transition(UP, 0, 34, 'Map1', 15),),
  },
  'Map4' : {
    RIGHT : (Transition(RIGHT, 0, 19, 'Map1', 0),),
    DOWN : (Transition(DOWN, 0, 79, 'Map2', 0),),
  },
  'Map5' : {
    LEFT : (Transition(LEFT, 27, 34, 'Map1', -17),
            Transition(LEFT, 3, 11, 'Map6', -3)),
  },
  'Map6' : {
    RIGHT : (Transition(RIGHT, 0, 19, 'Map5', 3),),
  },
}