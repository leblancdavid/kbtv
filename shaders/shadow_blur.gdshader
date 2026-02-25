shader_type canvas_item;

uniform float blur_amount : hint_range(0.0, 8.0) = 1.0;
uniform vec4 shadow_color : source_color = vec4(0.0, 0.0, 0.0, 0.5);
uniform float gradient_fade_height : hint_range(0.0, 1.0) = 0.5;
uniform bool gradient_at_top = false;

uniform vec4 room_bounds = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec2 shadow_aabb_min = vec2(0.0, 0.0);
uniform vec2 shadow_aabb_max = vec2(0.0, 0.0);

void fragment() {
    // Get texture alpha
    vec4 tex_color = texture(TEXTURE, UV);
    float tex_alpha = tex_color.a;
    
    // Clip shadow pixels outside room bounds and shadow AABB
    if (room_bounds.z > 0.0 && room_bounds.w > 0.0 && shadow_aabb_max.x > shadow_aabb_min.x) {
        vec2 texture_size = vec2(textureSize(TEXTURE, 0));
        
        // Calculate world position based on AABB
        vec2 world_pos = mix(shadow_aabb_min, shadow_aabb_max, UV);
        
        // First check if inside shadow AABB, then check if inside room bounds
        if (world_pos.x < shadow_aabb_min.x || world_pos.x > shadow_aabb_max.x ||
            world_pos.y < shadow_aabb_min.y || world_pos.y > shadow_aabb_max.y ||
            world_pos.x < room_bounds.x || world_pos.x > room_bounds.x + room_bounds.z ||
            world_pos.y < room_bounds.y || world_pos.y > room_bounds.y + room_bounds.w) {
            discard;
        }
    }
    
    // Calculate dual gradient (fade on both top and bottom edges)
    float fade_top = smoothstep(0.0, gradient_fade_height, UV.y);
    float fade_bottom = 1.0 - smoothstep(1.0 - gradient_fade_height, 1.0, UV.y);
    float gradient = fade_top * fade_bottom;
    
    // Multiply texture alpha by gradient
    float faded_alpha = tex_alpha * gradient;
    
    // Blur the faded alpha
    float blurred_alpha = 0.0;
    float total = 0.0;
    float offset = blur_amount;
    
    for (float x = -3.0; x <= 3.0; x += 1.0) {
        for (float y = -3.0; y <= 3.0; y += 1.0) {
            vec2 uv_offset = vec2(x, y) * offset / vec2(textureSize(TEXTURE, 0));
            vec2 sample_uv = UV + uv_offset;
            
            // Clamp sample_uv to valid range
            sample_uv = clamp(sample_uv, vec2(0.0), vec2(1.0));
            
            vec4 sample_tex = texture(TEXTURE, sample_uv);
            float sample_fade_top = smoothstep(0.0, gradient_fade_height, sample_uv.y);
            float sample_fade_bottom = 1.0 - smoothstep(1.0 - gradient_fade_height, 1.0, sample_uv.y);
            float sample_gradient = sample_fade_top * sample_fade_bottom;
            
            float sample_alpha = sample_tex.a * sample_gradient;
            
            float weight = 1.0 - length(vec2(x, y)) / 4.24;
            weight = max(weight, 0.0);
            
            blurred_alpha += sample_alpha * weight;
            total += weight;
        }
    }
    
    blurred_alpha /= total;
    
    // Apply shadow color
    vec4 color = vec4(shadow_color.rgb, shadow_color.a * blurred_alpha);
    COLOR = color;
}
