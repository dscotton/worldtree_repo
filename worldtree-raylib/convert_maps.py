"""Converts Python map data to JSON for the C# Raylib port."""

import json
import sys
import os

worldtree_dir = os.path.join(os.path.dirname(__file__), '..', 'worldtree')
sys.path.insert(0, worldtree_dir)

# Stub out the controller module so map_transitions can import it without pygame.
import types
controller_stub = types.ModuleType('controller')
controller_stub.UP = 1
controller_stub.DOWN = 2
controller_stub.LEFT = 3
controller_stub.RIGHT = 4
sys.modules['controller'] = controller_stub

import map_data
import map_data2
import map_transitions

os.makedirs(os.path.join(os.path.dirname(__file__), 'data'), exist_ok=True)
out = os.path.join(os.path.dirname(__file__), 'data')

with open(os.path.join(out, 'map_data.json'), 'w') as f:
    json.dump(map_data.map_data, f)

with open(os.path.join(out, 'map_data2.json'), 'w') as f:
    json.dump(map_data2.map_data, f)

# Transitions: convert Transition objects to dicts.
# Key structure: { region_str: { room: { direction_str: [ {first,last,region,dest,offset} ] } } }
direction_names = {
    map_transitions.LEFT: 'LEFT',
    map_transitions.RIGHT: 'RIGHT',
    map_transitions.UP: 'UP',
    map_transitions.DOWN: 'DOWN',
}
trans_out = {}
for region, rooms in map_transitions.transitions.items():
    trans_out[str(region)] = {}
    for room, dirs in rooms.items():
        trans_out[str(region)][room] = {}
        for direction, trans_list in dirs.items():
            dir_name = direction_names[direction]
            trans_out[str(region)][room][dir_name] = [
                {'first': t.first, 'last': t.last, 'region': t.region,
                 'dest': t.dest, 'offset': t.offset}
                for t in trans_list
            ]

with open(os.path.join(out, 'map_transitions.json'), 'w') as f:
    json.dump(trans_out, f)

print("Done. Generated data/map_data.json, data/map_data2.json, data/map_transitions.json")
