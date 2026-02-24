shader_type canvas_item;

varying float object_center_y;

uniform vec2 light_position = vec2(320.0, 180.0);
uniform float light_radius = 350.0;
uniform float max_brightness = 2.0;
uniform float shadow_factor = 0.5;

void vertex() {
    // Get the object's world Y position from the model matrix (column 3 is translation)
    object_center_y = MODEL_MATRIX[3].y;
}

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    
    // Calculate Y distance from light
    float y_distance = object_center_y - light_position.y;
    
    // Normalize by light_radius (0.0 = at light, 1.0 = at max distance)
    float distance_factor = abs(y_distance) / light_radius;
    distance_factor = clamp(distance_factor, 0.0, 1.0);
    
    // Apply linear scaling based on Y distance
    float brightness;
    if (y_distance > 0.0) {
        // Below light: progressively darker (further = more dark)
        brightness = mix(1.0, 1.0 - shadow_factor, distance_factor);
    } else {
        // Above light: progressively brighter (further = more bright)
        brightness = mix(1.0, max_brightness, distance_factor);
    }
    
    color.rgb *= brightness;
    
    COLOR = color;
}
