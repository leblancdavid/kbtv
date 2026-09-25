shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_nearest;
uniform float effect_strength = 0.50;
uniform int posterize_steps = 6;
uniform float saturation = 1.12;
uniform float outline_threshold = 0.52;
uniform float outline_bias = 0.10;
uniform vec4 outline_color : source_color = vec4(0.02, 0.03, 0.07, 1.0);
uniform float outline_mix = 0.70;
uniform float halftone_size_px = 8.0;
uniform float halftone_strength = 0.04;
uniform vec2 halftone_band = vec2(0.10, 0.72);

float luminance(vec3 color) {
	return dot(color, vec3(0.299, 0.587, 0.114));
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 px = SCREEN_PIXEL_SIZE;
	vec3 center = texture(screen_tex, uv).rgb;
	vec3 original = center;

	float tl = luminance(texture(screen_tex, uv + vec2(-px.x, -px.y)).rgb);
	float tc = luminance(texture(screen_tex, uv + vec2(0.0, -px.y)).rgb);
	float tr = luminance(texture(screen_tex, uv + vec2(px.x, -px.y)).rgb);
	float ml = luminance(texture(screen_tex, uv + vec2(-px.x, 0.0)).rgb);
	float mr = luminance(texture(screen_tex, uv + vec2(px.x, 0.0)).rgb);
	float bl = luminance(texture(screen_tex, uv + vec2(-px.x, px.y)).rgb);
	float bc = luminance(texture(screen_tex, uv + vec2(0.0, px.y)).rgb);
	float br = luminance(texture(screen_tex, uv + vec2(px.x, px.y)).rgb);

	float gx = -tl - 2.0 * ml - bl + tr + 2.0 * mr + br;
	float gy = -tl - 2.0 * tc - tr + bl + 2.0 * bc + br;
	float edge = length(vec2(gx, gy));

	float base_luma = max(luminance(center), 0.0001);
	float steps = max(float(posterize_steps), 1.0);
	float band_luma = floor(base_luma * steps + 0.5) / steps;
	vec3 color = center * (band_luma / base_luma);
	color = mix(vec3(luminance(color)), color, saturation);

	vec2 halftone_uv = (uv / px) / max(halftone_size_px, 1.0);
	float dot_value = smoothstep(0.5, 0.05, distance(fract(halftone_uv), vec2(0.5)));
	float halftone_mask = smoothstep(halftone_band.x, halftone_band.x + 0.10, band_luma)
		* (1.0 - smoothstep(halftone_band.y - 0.10, halftone_band.y, band_luma));
	color *= mix(vec3(1.0), vec3(dot_value), halftone_mask * halftone_strength);

	float ink = smoothstep(outline_threshold, outline_threshold + outline_bias, edge) * outline_mix;
	color = mix(color, outline_color.rgb, clamp(ink, 0.0, 1.0));
	color = mix(original, color, clamp(effect_strength, 0.0, 1.0));

	COLOR = vec4(color, 1.0);
}
