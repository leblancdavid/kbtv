shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_nearest;

uniform float desaturation : hint_range(0.0, 1.0) = 0.45;
uniform float gamma : hint_range(0.5, 2.0) = 1.18;
uniform float black_floor : hint_range(0.0, 0.2) = 0.005;
uniform float vignette_inner : hint_range(0.0, 1.0) = 0.50;
uniform float vignette_outer : hint_range(0.0, 1.0) = 0.95;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.45;

uniform float grain_strength : hint_range(0.0, 0.3) = 0.04;
uniform float scanline_strength : hint_range(0.0, 0.2) = 0.03;
uniform float scanline_scale : hint_range(50.0, 800.0) = 360.0;

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

void fragment() {
	vec3 col = texture(screen_tex, SCREEN_UV).rgb;

	float lum = dot(col, vec3(0.299, 0.587, 0.114));
	col = mix(col, vec3(lum), desaturation);

	col = pow(max(col, vec3(black_floor)), vec3(1.0 / gamma));
	col = max(col, vec3(black_floor));

	vec2 centered = SCREEN_UV - vec2(0.5);
	float dist = length(centered) * 1.4142135;
	float vignette = 1.0 - smoothstep(vignette_inner, vignette_outer, dist) * vignette_strength;
	col *= vignette;

	float grain = hash(SCREEN_UV * vec2(1920.0, 1080.0) + vec2(TIME * 13.0, TIME * 7.0));
	col += (grain - 0.5) * grain_strength;

	float scan = sin(SCREEN_UV.y * scanline_scale) * 0.5 + 0.5;
	col *= 1.0 - scanline_strength * (1.0 - scan);

	COLOR = vec4(col, 1.0);
}