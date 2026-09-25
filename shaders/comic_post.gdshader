shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_nearest;
uniform float effect_strength = 0.78;
uniform int posterize_steps = 5;
uniform float saturation = 1.22;
uniform float outline_threshold = 0.22;
uniform float outline_bias = 0.07;
uniform float outline_width_px = 2.0;
uniform vec4 outline_color : source_color = vec4(0.02, 0.03, 0.07, 1.0);
uniform float outline_mix = 1.0;
uniform float halftone_size_px = 14.0;
uniform float halftone_strength = 0.0;
uniform vec2 halftone_band = vec2(0.30, 0.72);
uniform bool median_filter_enabled = true;
uniform float median_filter_strength = 0.25;
uniform float median_filter_threshold = 0.08;
uniform float median_filter_radius_px = 1.5;
uniform bool shadow_flatten_enabled = false;
uniform float shadow_flatten_threshold = 0.30;
uniform float shadow_flatten_softness = 0.035;
uniform float shadow_flatten_strength = 0.0;
uniform bool shadow_smooth_enabled = true;
uniform float shadow_smooth_threshold = 0.55;
uniform float shadow_smooth_softness = 0.14;
uniform float shadow_smooth_edge_reject = 0.34;
uniform float shadow_smooth_edge_softness = 0.12;
uniform float shadow_smooth_strength = 1.0;
uniform float shadow_smooth_radius_px = 4.0;

float luminance(vec3 color) {
	return dot(color, vec3(0.299, 0.587, 0.114));
}

void sort_pair(inout float a, inout float b) {
	float low = min(a, b);
	float high = max(a, b);
	a = low;
	b = high;
}

float median9(float a, float b, float c, float d, float e, float f, float g, float h, float i) {
	sort_pair(a, b); sort_pair(a, c); sort_pair(a, d); sort_pair(a, e);
	sort_pair(a, f); sort_pair(a, g); sort_pair(a, h); sort_pair(a, i);
	sort_pair(b, c); sort_pair(b, d); sort_pair(b, e); sort_pair(b, f);
	sort_pair(b, g); sort_pair(b, h); sort_pair(b, i);
	sort_pair(c, d); sort_pair(c, e); sort_pair(c, f); sort_pair(c, g);
	sort_pair(c, h); sort_pair(c, i);
	sort_pair(d, e); sort_pair(d, f); sort_pair(d, g); sort_pair(d, h);
	sort_pair(d, i);
	sort_pair(e, f); sort_pair(e, g); sort_pair(e, h); sort_pair(e, i);
	return e;
}

float median_clamp(float value, float median_value) {
	if (!median_filter_enabled) {
		return value;
	}

	float outlier = step(max(median_filter_threshold, 0.0), abs(value - median_value));
	return mix(value, median_value, outlier * clamp(median_filter_strength, 0.0, 1.0));
}

void pick_closer_to_luma(
	inout vec3 best_color,
	inout float best_delta,
	vec3 candidate_color,
	float candidate_luma,
	float target_luma
) {
	float candidate_delta = abs(candidate_luma - target_luma);
	if (candidate_delta < best_delta) {
		best_color = candidate_color;
		best_delta = candidate_delta;
	}
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 edge_px = SCREEN_PIXEL_SIZE * max(outline_width_px, 1.0);
	vec2 filter_px = SCREEN_PIXEL_SIZE * max(median_filter_radius_px, 1.0);
	vec2 smooth_px = SCREEN_PIXEL_SIZE * max(shadow_smooth_radius_px, 1.0);
	vec3 center = texture(screen_tex, uv).rgb;
	vec3 smooth_tl = texture(screen_tex, uv + vec2(-smooth_px.x, -smooth_px.y)).rgb;
	vec3 smooth_n = texture(screen_tex, uv + vec2(0.0, -smooth_px.y)).rgb;
	vec3 smooth_tr = texture(screen_tex, uv + vec2(smooth_px.x, -smooth_px.y)).rgb;
	vec3 smooth_s = texture(screen_tex, uv + vec2(0.0, smooth_px.y)).rgb;
	vec3 smooth_w = texture(screen_tex, uv + vec2(-smooth_px.x, 0.0)).rgb;
	vec3 smooth_e = texture(screen_tex, uv + vec2(smooth_px.x, 0.0)).rgb;
	vec3 smooth_bl = texture(screen_tex, uv + vec2(-smooth_px.x, smooth_px.y)).rgb;
	vec3 smooth_br = texture(screen_tex, uv + vec2(smooth_px.x, smooth_px.y)).rgb;
	vec3 color_tl = texture(screen_tex, uv + vec2(-filter_px.x, -filter_px.y)).rgb;
	vec3 color_tc = texture(screen_tex, uv + vec2(0.0, -filter_px.y)).rgb;
	vec3 color_tr = texture(screen_tex, uv + vec2(filter_px.x, -filter_px.y)).rgb;
	vec3 color_ml = texture(screen_tex, uv + vec2(-filter_px.x, 0.0)).rgb;
	vec3 color_mr = texture(screen_tex, uv + vec2(filter_px.x, 0.0)).rgb;
	vec3 color_bl = texture(screen_tex, uv + vec2(-filter_px.x, filter_px.y)).rgb;
	vec3 color_bc = texture(screen_tex, uv + vec2(0.0, filter_px.y)).rgb;
	vec3 color_br = texture(screen_tex, uv + vec2(filter_px.x, filter_px.y)).rgb;

	float tl = luminance(color_tl);
	float tc = luminance(color_tc);
	float tr = luminance(color_tr);
	float ml = luminance(color_ml);
	float mr = luminance(color_mr);
	float bl = luminance(color_bl);
	float bc = luminance(color_bc);
	float br = luminance(color_br);
	float mc = luminance(center);
	float median_luma = median9(tl, tc, tr, ml, mc, mr, bl, bc, br);
	vec3 median_color = center;
	float median_color_delta = abs(mc - median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_tl, tl, median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_tc, tc, median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_tr, tr, median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_ml, ml, median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_mr, mr, median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_bl, bl, median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_bc, bc, median_luma);
	pick_closer_to_luma(median_color, median_color_delta, color_br, br, median_luma);

	float center_outlier = median_filter_enabled
		? step(max(median_filter_threshold, 0.0), abs(mc - median_luma))
		: 0.0;
	center = mix(center, median_color, center_outlier * clamp(median_filter_strength, 0.0, 1.0));
	vec3 original = center;

	mc = median_clamp(mc, median_luma);

	float etl = median_clamp(luminance(texture(screen_tex, uv + vec2(-edge_px.x, -edge_px.y)).rgb), median_luma);
	float etc = median_clamp(luminance(texture(screen_tex, uv + vec2(0.0, -edge_px.y)).rgb), median_luma);
	float etr = median_clamp(luminance(texture(screen_tex, uv + vec2(edge_px.x, -edge_px.y)).rgb), median_luma);
	float eml = median_clamp(luminance(texture(screen_tex, uv + vec2(-edge_px.x, 0.0)).rgb), median_luma);
	float emr = median_clamp(luminance(texture(screen_tex, uv + vec2(edge_px.x, 0.0)).rgb), median_luma);
	float ebl = median_clamp(luminance(texture(screen_tex, uv + vec2(-edge_px.x, edge_px.y)).rgb), median_luma);
	float ebc = median_clamp(luminance(texture(screen_tex, uv + vec2(0.0, edge_px.y)).rgb), median_luma);
	float ebr = median_clamp(luminance(texture(screen_tex, uv + vec2(edge_px.x, edge_px.y)).rgb), median_luma);

	float gx = -etl - 2.0 * eml - ebl + etr + 2.0 * emr + ebr;
	float gy = -etl - 2.0 * etc - etr + ebl + 2.0 * ebc + ebr;
	float edge = length(vec2(gx, gy));

	float base_luma = max(mc, 0.0001);
	float steps = max(float(posterize_steps), 1.0);
	float band_luma = floor(base_luma * steps + 0.5) / steps;
	vec3 color = center * (band_luma / base_luma);
	color = mix(vec3(luminance(color)), color, saturation);
	vec3 smooth_source = (center * 4.0 + smooth_n * 2.0 + smooth_s * 2.0 + smooth_w * 2.0 + smooth_e * 2.0
		+ smooth_tl + smooth_tr + smooth_bl + smooth_br) / 16.0;
	float smooth_luma = max(luminance(smooth_source), 0.0001);
	vec3 smooth_color = smooth_source * (band_luma / smooth_luma);
	smooth_color = mix(vec3(luminance(smooth_color)), smooth_color, saturation);
	float shadow_smooth_mask = shadow_smooth_enabled
		? (1.0 - smoothstep(
			shadow_smooth_threshold,
			shadow_smooth_threshold + max(shadow_smooth_softness, 0.0001),
			band_luma))
			* (1.0 - smoothstep(
				shadow_smooth_edge_reject,
				shadow_smooth_edge_reject + max(shadow_smooth_edge_softness, 0.0001),
				edge))
			* clamp(shadow_smooth_strength, 0.0, 1.0)
		: 0.0;
	color = mix(color, smooth_color, shadow_smooth_mask);
	float shadow_mask = shadow_flatten_enabled
		? (1.0 - smoothstep(
			shadow_flatten_threshold,
			shadow_flatten_threshold + max(shadow_flatten_softness, 0.0001),
			band_luma)) * clamp(shadow_flatten_strength, 0.0, 1.0)
		: 0.0;
	vec3 flat_shadow = mix(vec3(luminance(color)), outline_color.rgb, 0.80);
	color = mix(color, flat_shadow, shadow_mask);

	vec2 halftone_uv = (uv / edge_px) / max(halftone_size_px, 1.0);
	float dot_value = smoothstep(0.5, 0.05, distance(fract(halftone_uv), vec2(0.5)));
	float halftone_mask = smoothstep(halftone_band.x, halftone_band.x + 0.10, band_luma)
		* (1.0 - smoothstep(halftone_band.y - 0.10, halftone_band.y, band_luma));
	color *= mix(vec3(1.0), vec3(dot_value), halftone_mask * (1.0 - shadow_mask) * halftone_strength);

	float ink = smoothstep(outline_threshold, outline_threshold + outline_bias, edge) * outline_mix;
	color = mix(color, outline_color.rgb, clamp(ink, 0.0, 1.0));
	color = mix(original, color, max(clamp(effect_strength, 0.0, 1.0), shadow_smooth_mask));

	COLOR = vec4(color, 1.0);
}
