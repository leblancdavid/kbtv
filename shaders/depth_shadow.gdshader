shader_type canvas_item;

varying float world_y;

uniform float light_y = 80.0;
uniform float shadow_strength : hint_range(0.0, 1.0) = 0.5;
uniform float transition_size : hint_range(4.0, 128.0) = 16.0;

void vertex() {
    // Calculate world Y in vertex shader
    vec2 world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
    world_y = world_pos.y;
}

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    
    // Use world_y from vertex shader
    // Invert: light_y - world_y so objects BELOW light are shadowed
    float shadow_factor = clamp((light_y - world_y + transition_size) / (2.0 * transition_size), 0.0, 1.0);
    
    // Apply shadow by darkening the color
    float brightness = mix(1.0 - shadow_strength, 1.0, shadow_factor);
    color.rgb *= brightness;
    
    COLOR = color;
}
