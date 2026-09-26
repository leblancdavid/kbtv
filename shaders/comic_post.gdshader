shader_type spatial;
render_mode unshaded, fog_disabled, cull_disabled, depth_test_disabled, depth_draw_never;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear;
uniform sampler2D depth_tex : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D normal_roughness_tex : hint_normal_roughness_texture, repeat_disable, filter_nearest;
uniform float effect_strength = 0.90;
uniform int posterize_steps = 5;
uniform float saturation = 0.92;
uniform float outline_threshold = 0.14;
uniform float outline_bias = 0.045;
uniform float outline_width_px = 2.0;
uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float outline_mix = 1.2;
uniform bool depth_edges_enabled = true;
uniform float depth_edge_mix = 1.0;
uniform float depth_edge_threshold = 0.045;
uniform float depth_edge_bias = 0.018;
uniform float depth_edge_width_px = 2.0;
uniform bool normal_edges_enabled = true;
uniform float normal_edge_mix = 0.35;
uniform float normal_edge_threshold = 0.10;
uniform float normal_edge_bias = 0.10;
// Supplied by ComicPostLayer from the active Camera3D for far-plane silhouette checks.
uniform vec2 camera_near_far = vec2(0.05, 100.0);

uniform float luma_edge_mix = 0.18;
uniform float detail_ink_suppression = 0.92;
uniform float bright_detail_ink_suppression = 0.75;
uniform float bright_detail_threshold = 0.34;
uniform float bright_detail_softness = 0.18;
uniform float macro_edge_width_px = 7.0;
uniform float macro_edge_threshold = 0.16;
uniform float macro_edge_bias = 0.10;
uniform float halftone_size_px = 14.0;
uniform float halftone_strength = 0.0;
uniform vec2 halftone_band = vec2(0.30, 0.72);
uniform bool median_filter_enabled = true;
uniform float median_filter_strength = 0.25;
uniform float median_filter_threshold = 0.08;
uniform float median_filter_radius_px = 1.5;
uniform float sobel_prefilter_strength = 0.65;
uniform bool shadow_flatten_enabled = false;
uniform float shadow_flatten_threshold = 0.30;
uniform float shadow_flatten_softness = 0.035;
uniform float shadow_flatten_strength = 0.0;
uniform bool shadow_smooth_enabled = true;
uniform float shadow_smooth_threshold = 0.55;
uniform float shadow_smooth_softness = 0.14;
uniform float shadow_smooth_edge_reject = 0.34;
uniform float shadow_smooth_edge_softness = 0.12;
uniform float shadow_smooth_strength = 0.0;
uniform float shadow_smooth_radius_px = 4.0;

float luminance(vec3 color) {
	return dot(color, vec3(0.299, 0.587, 0.114));
}

vec3 clamp_rgb(vec3 color) {
	return clamp(color, vec3(0.0), vec3(1.0));
}

float capped_luma_scale(float target_luma, float source_luma) {
	return min(target_luma / max(source_luma, 0.0001), 4.0);
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

float prefilter_luma(float center_value, float north_value, float south_value, float west_value, float east_value) {
	float cross_average = (center_value * 4.0 + north_value + south_value + west_value + east_value) / 8.0;
	return mix(center_value, cross_average, clamp(sobel_prefilter_strength, 0.0, 1.0));
}

float sobel_luma(vec2 uv, vec2 px) {
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
	return length(vec2(gx, gy));
}

// Godot 4.3+ uses a reverse-Z depth buffer in Forward+/Mobile: depth is 1.0 at
// the near plane and 0.0 at the far plane. Reconstruct view distance with the
// inverse projection matrix instead of treating depth as standard OpenGL Z.
float linearize_depth(vec2 sample_uv, float raw_depth, mat4 inv_projection_matrix) {
#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(sample_uv, raw_depth) * 2.0 - 1.0;
#else
	vec3 ndc = vec3(sample_uv * 2.0 - 1.0, raw_depth);
#endif
	vec4 view = inv_projection_matrix * vec4(ndc, 1.0);
	view.xyz /= view.w;
	return -view.z;
}

bool is_far_plane(float linear_depth, float far_plane) {
	return linear_depth >= far_plane * 0.995;
}

// Relative depth jump: the change in distance as a fraction of the farther of
// the two samples, so a slanted floor stays clean at any camera distance. A
// sample sitting on the far plane means background, which is always a
// silhouette worth inking.
float relative_depth_delta(float center_depth, vec2 sample_uv, float raw_depth, float far_plane, mat4 inv_projection_matrix) {
	float sample_depth = linearize_depth(sample_uv, raw_depth, inv_projection_matrix);
	if (is_far_plane(sample_depth, far_plane)) {
		return 1.0;
	}
	float scale = max(max(center_depth, sample_depth), 0.0001);
	return abs(sample_depth - center_depth) / scale;
}

// Roberts cross: two diagonal taps instead of all eight neighbours, so a
// silhouette costs half the reads and a flat surface contributes no gradient.
float depth_edge_delta(vec2 uv, vec2 px, float far_plane, mat4 inv_projection_matrix) {
	float center_depth = linearize_depth(uv, texture(depth_tex, uv).r, inv_projection_matrix);
	if (is_far_plane(center_depth, far_plane)) {
		return 0.0;
	}
	vec2 diagonal_a_uv = uv + vec2(px.x, px.y);
	vec2 diagonal_b_uv = uv - vec2(px.x, px.y);
	float diagonal_a = relative_depth_delta(center_depth, diagonal_a_uv, texture(depth_tex, diagonal_a_uv).r, far_plane, inv_projection_matrix);
	float diagonal_b = relative_depth_delta(center_depth, diagonal_b_uv, texture(depth_tex, diagonal_b_uv).r, far_plane, inv_projection_matrix);
	return max(diagonal_a, diagonal_b);
}

vec3 read_normal(vec2 uv) {
	vec3 raw = texture(normal_roughness_tex, uv).rgb * 2.0 - 1.0;
	float len = length(raw);
	// An unwritten normal buffer samples as zero; normalize() would give NaN.
	return len > 0.0001 ? raw / len : vec3(0.0, 1.0, 0.0);
}

// Crease severity as 1 - cos(angle): 0 when coplanar, 0.06 at 20 degrees,
// 0.29 at 45. Dot products keep this in a 0..2 range regardless of normal
// encoding, unlike the length of a normal difference.
float normal_crease_delta(vec2 uv, vec2 px) {
	vec3 c = read_normal(uv);
	float diagonal_a = 1.0 - dot(c, read_normal(uv + vec2(px.x, px.y)));
	float diagonal_b = 1.0 - dot(c, read_normal(uv - vec2(px.x, px.y)));
	return max(diagonal_a, diagonal_b);
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

void vertex() {
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 screen_px = 1.0 / VIEWPORT_SIZE;
	vec2 edge_px = screen_px * max(outline_width_px, 1.0);
	vec2 depth_px = screen_px * max(depth_edge_width_px, 1.0);
	vec2 macro_px = screen_px * max(macro_edge_width_px, outline_width_px);
	vec2 filter_px = screen_px * max(median_filter_radius_px, 1.0);
	vec2 smooth_px = screen_px * max(shadow_smooth_radius_px, 1.0);
	mat4 inv_projection_matrix = INV_PROJECTION_MATRIX;
	float far_plane = camera_near_far.y;
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
	float emc = median_clamp(luminance(center), median_luma);
	float emr = median_clamp(luminance(texture(screen_tex, uv + vec2(edge_px.x, 0.0)).rgb), median_luma);
	float ebl = median_clamp(luminance(texture(screen_tex, uv + vec2(-edge_px.x, edge_px.y)).rgb), median_luma);
	float ebc = median_clamp(luminance(texture(screen_tex, uv + vec2(0.0, edge_px.y)).rgb), median_luma);
	float ebr = median_clamp(luminance(texture(screen_tex, uv + vec2(edge_px.x, edge_px.y)).rgb), median_luma);

	etl = prefilter_luma(etl, etl, eml, etl, etc);
	etc = prefilter_luma(etc, etc, emc, etl, etr);
	etr = prefilter_luma(etr, etr, emr, etc, etr);
	eml = prefilter_luma(eml, etl, ebl, eml, emc);
	emr = prefilter_luma(emr, etr, ebr, emc, emr);
	ebl = prefilter_luma(ebl, eml, ebl, ebl, ebc);
	ebc = prefilter_luma(ebc, emc, ebc, ebl, ebr);
	ebr = prefilter_luma(ebr, emr, ebr, ebc, ebr);

	float gx = -etl - 2.0 * eml - ebl + etr + 2.0 * emr + ebr;
	float gy = -etl - 2.0 * etc - etr + ebl + 2.0 * ebc + ebr;
	float edge = length(vec2(gx, gy));

	float base_luma = max(mc, 0.0001);
	float steps = max(float(posterize_steps), 1.0);
	float quantized_luma = floor(base_luma * steps + 0.5) / steps;
	float center_linear_depth = linearize_depth(uv, texture(depth_tex, uv).r, inv_projection_matrix);
	float surface_mask = is_far_plane(center_linear_depth, far_plane) ? 0.0 : 1.0;
	// Noir lighting keeps many textured surfaces below the first posterize band.
	// Give real geometry a hard dark band while keeping far-plane background black.
	float dark_surface_band = surface_mask * 0.060 * step(0.004, base_luma);
	float band_luma = max(quantized_luma, dark_surface_band);
	vec3 color = clamp_rgb(center * capped_luma_scale(band_luma, base_luma));
	color = mix(vec3(luminance(color)), color, saturation);
	color = clamp_rgb(color);
	vec3 smooth_source = (center * 4.0 + smooth_n * 2.0 + smooth_s * 2.0 + smooth_w * 2.0 + smooth_e * 2.0
		+ smooth_tl + smooth_tr + smooth_bl + smooth_br) / 16.0;
	float smooth_luma = max(luminance(smooth_source), 0.0001);
	vec3 smooth_color = clamp_rgb(smooth_source * capped_luma_scale(band_luma, smooth_luma));
	smooth_color = mix(vec3(luminance(smooth_color)), smooth_color, saturation);
	smooth_color = clamp_rgb(smooth_color);
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

	float macro_edge = sobel_luma(uv, macro_px);
	float macro_gate = smoothstep(
		macro_edge_threshold,
		macro_edge_threshold + max(macro_edge_bias, 0.0001),
		macro_edge);
	// High-frequency, non-macro contrast gets a small amount of local color detail
	// back so controls/tracks stay readable after broad posterization.
	float fine_detail = smoothstep(0.035, 0.12, edge) * (1.0 - macro_gate);
	float original_luma = luminance(original);
	vec3 original_chroma = original - vec3(original_luma);
	vec3 detail_boost = color
		+ original_chroma * 0.16
		+ vec3(original_luma - luminance(color)) * 0.18;
	color = mix(color, clamp_rgb(detail_boost), fine_detail * 0.40);
	float bright_detail_mask = smoothstep(
		bright_detail_threshold,
		bright_detail_threshold + max(bright_detail_softness, 0.0001),
		band_luma);
	float combined_detail_suppression = clamp(
		detail_ink_suppression + bright_detail_mask * bright_detail_ink_suppression,
		0.0,
		1.0);
	float detail_gate = mix(1.0, 1.0 - macro_gate, combined_detail_suppression);
	float luma_ink = smoothstep(outline_threshold, outline_threshold + max(outline_bias, 0.0001), edge)
		* detail_gate
		* clamp(luma_edge_mix, 0.0, 1.0);
	float center_luma = luminance(center);
	float center_chroma = length(center - vec3(center_luma));
	float bright_light_gradient = smoothstep(0.42, 0.78, center_luma)
		* (1.0 - smoothstep(0.04, 0.14, center_chroma))
		* macro_gate;
	luma_ink *= mix(1.0, 0.06, bright_light_gradient);
	// Red/warm-lit skin details can look like dense hatch marks. Suppress only
	// luma/normal ink there; depth silhouettes still draw normally.
	float warm_face_like = smoothstep(0.08, 0.22, center.r - max(center.g, center.b))
		* smoothstep(0.08, 0.35, center_luma)
		* (1.0 - smoothstep(0.55, 0.85, center_luma));
	luma_ink *= mix(1.0, 0.35, warm_face_like);
	float depth_ink = depth_edges_enabled
		? smoothstep(
			depth_edge_threshold,
			depth_edge_threshold + max(depth_edge_bias, 0.0001),
			depth_edge_delta(uv, depth_px, far_plane, inv_projection_matrix))
			* clamp(depth_edge_mix, 0.0, 1.0)
		: 0.0;
	float normal_ink = normal_edges_enabled
		? smoothstep(
			normal_edge_threshold,
			normal_edge_threshold + max(normal_edge_bias, 0.0001),
			normal_crease_delta(uv, edge_px))
			* clamp(normal_edge_mix, 0.0, 1.0)
		: 0.0;
	normal_ink *= mix(1.0, 0.55, warm_face_like);
	float ink = max(max(luma_ink, depth_ink), normal_ink) * outline_mix;
	ink = smoothstep(0.03, 0.35, ink);
	float noir_shadow = 1.0 - smoothstep(0.30, 0.80, band_luma);
	vec3 noir_tint = vec3(0.78, 0.82, 0.92);
	color *= mix(vec3(1.0), noir_tint * 0.68, noir_shadow);
	color = clamp_rgb(color);
	color = mix(original, color, clamp(effect_strength, 0.0, 1.0));
	// Ink last so strong edges are fully black instead of being diluted by the
	// original-image blend.
	color = clamp_rgb(mix(color, outline_color.rgb, clamp(ink, 0.0, 1.0)));

	ALBEDO = color;
	EMISSION = vec3(0.0);
	ALPHA = 1.0;
}
