shader_type spatial;

uniform sampler2D albedo_textures[4];
uniform sampler2D normal_textures[4];
uniform sampler2D roughness_textures[4];
uniform float texture_scale = 32.0; // Add a scale uniform

varying vec4 splat_weights;

void vertex() {
    splat_weights = COLOR;
}

void fragment() {
// Scale the UVs
    vec2 scaled_uv = UV / texture_scale;

    vec3 albedo = vec3(0.0);
    vec3 normal = vec3(0.0);
    float roughness = 0.0;

    albedo += texture(albedo_textures[0], scaled_uv).rgb * splat_weights.r;
    albedo += texture(albedo_textures[1], scaled_uv).rgb * splat_weights.g;
    albedo += texture(albedo_textures[2], scaled_uv).rgb * splat_weights.b;
    albedo += texture(albedo_textures[3], scaled_uv).rgb * splat_weights.a;

    vec3 normal_map = vec3(0.0);
    normal_map += texture(normal_textures[0], scaled_uv).rgb * splat_weights.r;
    normal_map += texture(normal_textures[1], scaled_uv).rgb * splat_weights.g;
    normal_map += texture(normal_textures[2], scaled_uv).rgb * splat_weights.b;
    normal_map += texture(normal_textures[3], scaled_uv).rgb * splat_weights.a;

    roughness += texture(roughness_textures[0], scaled_uv).r * splat_weights.r;
    roughness += texture(roughness_textures[1], scaled_uv).r * splat_weights.g;
    roughness += texture(roughness_textures[2], scaled_uv).r * splat_weights.b;
    roughness += texture(roughness_textures[3], scaled_uv).r * splat_weights.a;

    ALBEDO = albedo;
    NORMAL_MAP = normal_map;
    ROUGHNESS = roughness;
    ALPHA = 1.0;
}