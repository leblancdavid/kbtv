shader_type canvas_item;

uniform vec2 player_position = vec2(0.0, 0.0);
uniform float object_height : hint_range(0.0, 128.0) = 32.0;
uniform float fade_range : hint_range(0.0, 64.0) = 16.0;
uniform bool occluder_enabled = true;
uniform float custom_alpha : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	vec4 tex_color = texture(TEXTURE, UV);
	
	if (tex_color.a == 0.0) {
		COLOR = tex_color;
		return;
	}
	
	if (!occluder_enabled) {
		COLOR = tex_color;
		return;
	}
	
	vec2 world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	
	float object_top = world_position.y + object_height;
	float distance_to_player = player_position.y - object_top;
	
	float alpha = 1.0;
	
	if (player_position.y > object_top) {
		alpha = clamp(distance_to_player / fade_range, 0.0, 1.0);
	}
	
	// Use custom_alpha for front wall occlusion
	alpha = alpha * custom_alpha;
	
	COLOR = tex_color;
	COLOR.a = alpha * tex_color.a;
}
