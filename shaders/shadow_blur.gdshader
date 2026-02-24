shader_type canvas_item;

uniform float blur_amount : hint_range(0.0, 8.0) = 2.0;
uniform vec4 shadow_color : source_color = vec4(0.0, 0.0, 0.0, 0.5);

void fragment() {
    vec4 color = vec4(0.0);
    
    float total = 0.0;
    float offset = blur_amount;
    
    // Simple box blur
    for (float x = -3.0; x <= 3.0; x += 1.0) {
        for (float y = -3.0; y <= 3.0; y += 1.0) {
            vec2 uv_offset = vec2(x, y) * offset / vec2(textureSize(TEXTURE, 0));
            vec4 tex_color = texture(TEXTURE, UV + uv_offset);
            float weight = 1.0 - length(vec2(x, y)) / 4.24;
            weight = max(weight, 0.0);
            color += tex_color * weight;
            total += weight;
        }
    }
    
    color /= total;
    
    // Apply shadow color (black with alpha)
    color.rgb = shadow_color.rgb;
    color.a *= shadow_color.a;
    
    COLOR = color;
}
