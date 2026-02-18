shader_type canvas_item;

uniform vec2 player_position = vec2(0.0, 0.0);
uniform float object_height : hint_range(0.0, 128.0) = 32.0;
uniform float fade_range : hint_range(0.0, 64.0) = 16.0;
uniform bool occluder_enabled = true;

void fragment() {
	if (!occluder_enabled) {
		COLOR.a = 1.0;
		return;
	}
	
	vec2 object_position = (COLOREXT.xy * (1.0 / TEXTURE_PIXEL_SIZE)) + VERTEX;
	vec2 world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	
	float object_top = world_position.y + object_height;
	float distance_to_player = player_position.y - object_top;
	
	float alpha = 1.0;
	
	if (player_position.y > object_top) {
		alpha = clamp(distance_to_player / fade_range, 0.0, 1.0);
	}
	
	COLOR.a = alpha * texture(TEXTURE, UV).a;
}
