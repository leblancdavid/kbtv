extends Node2D

@export var tile_set: TileSet

const GRID_WIDTH = 8
const GRID_HEIGHT = 5

const FLOOR_SOURCE = 0
const WALL_NORTH_SOURCE = 1
const WALL_SOUTH_SOURCE = 2
const WALL_WEST_SOURCE = 3
const WALL_EAST_SOURCE = 4

func _ready():
	var floor_layer = $FloorLayer
	if floor_layer == null or tile_set == null:
		push_error("ControlRoom: Missing FloorLayer or TileSet")
		return

	floor_layer.tile_set = tile_set
	
	# Paint floor and walls
	paint_floor(floor_layer)
	paint_walls(floor_layer)

func paint_floor(layer: TileMapLayer):
	for x in range(GRID_WIDTH):
		for y in range(GRID_HEIGHT):
			layer.set_cell(Vector2i(x, y), FLOOR_SOURCE, Vector2i(0, 0), 0)

func paint_walls(layer: TileMapLayer):
	# North wall (back) - along y = -1
	for x in range(GRID_WIDTH):
		layer.set_cell(Vector2i(x, -1), WALL_NORTH_SOURCE, Vector2i(0, 0), 0)
	
	# South wall (front) - along y = GRID_HEIGHT
	for x in range(GRID_WIDTH):
		layer.set_cell(Vector2i(x, GRID_HEIGHT), WALL_SOUTH_SOURCE, Vector2i(0, 0), 0)
	
	# West wall (left) - along x = -1
	for y in range(GRID_HEIGHT):
		layer.set_cell(Vector2i(-1, y), WALL_WEST_SOURCE, Vector2i(0, 0), 0)
	
	# East wall (right) - along x = GRID_WIDTH
	for y in range(GRID_HEIGHT):
		layer.set_cell(Vector2i(GRID_WIDTH, y), WALL_EAST_SOURCE, Vector2i(0, 0), 0)
