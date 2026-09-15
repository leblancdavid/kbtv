shader_type canvas_item;
render_mode unshaded, blend_premul_alpha;

uniform vec2 screen_size = vec2(1152.0, 640.0);
uniform float corner_radius = 16.0;
uniform float edge_feather = 10.0;
uniform float overall_alpha = 1.0;

float sd_round_box(vec2 p, vec2 b, float r) {
	vec2 q = abs(p) - b + r;
	return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - r;
}

void fragment() {
	vec2 p = (UV - vec2(0.5)) * screen_size;
	float d = sd_round_box(p, screen_size * 0.5, corner_radius);
	float edge_alpha = 1.0 - smoothstep(-edge_feather, 0.0, d);
	// Transparent viewport textures contain premultiplied RGB. Fade both
	// color and coverage together, then composite with premultiplied blending.
	COLOR = texture(TEXTURE, UV) * clamp(edge_alpha, 0.0, 1.0) * overall_alpha;
}
