shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform sampler2D albedo_textures[4];
uniform sampler2D normal_textures[4];
uniform sampler2D roughness_textures[4];
uniform sampler2D metallic_textures[4];
uniform float texture_scale = 32.0;

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
    float metallic = 0.0;

    // Albedo
    albedo += texture(albedo_textures[0], scaled_uv).rgb * splat_weights.r;
    albedo += texture(albedo_textures[1], scaled_uv).rgb * splat_weights.g;
    albedo += texture(albedo_textures[2], scaled_uv).rgb * splat_weights.b;
    albedo += texture(albedo_textures[3], scaled_uv).rgb * splat_weights.a;

    // Normal - properly decode normal maps
    //vec3 n1 = texture(normal_textures[0], scaled_uv).rgb * 2.0 - 1.0;
    //vec3 n2 = texture(normal_textures[1], scaled_uv).rgb * 2.0 - 1.0;
    //vec3 n3 = texture(normal_textures[2], scaled_uv).rgb * 2.0 - 1.0;
    //vec3 n4 = texture(normal_textures[3], scaled_uv).rgb * 2.0 - 1.0;
    //
    //normal = normalize(
        //n1 * splat_weights.r +
        //n2 * splat_weights.g +
        //n3 * splat_weights.b +
        //n4 * splat_weights.a
    //);
	normal += texture(normal_textures[0], scaled_uv).rgb * splat_weights.r;
normal += texture(normal_textures[1], scaled_uv).rgb * splat_weights.g;
normal += texture(normal_textures[2], scaled_uv).rgb * splat_weights.b;
normal += texture(normal_textures[3], scaled_uv).rgb * splat_weights.a;

    // Roughness
    roughness += texture(roughness_textures[0], scaled_uv).r * splat_weights.r;
    roughness += texture(roughness_textures[1], scaled_uv).r * splat_weights.g;
    roughness += texture(roughness_textures[2], scaled_uv).r * splat_weights.b;
    roughness += texture(roughness_textures[3], scaled_uv).r * splat_weights.a;

    // Metallic
    metallic += texture(metallic_textures[0], scaled_uv).r * splat_weights.r;
    metallic += texture(metallic_textures[1], scaled_uv).r * splat_weights.g;
    metallic += texture(metallic_textures[2], scaled_uv).r * splat_weights.b;
    metallic += texture(metallic_textures[3], scaled_uv).r * splat_weights.a;

    // Set output values
    ALBEDO = albedo;
    NORMAL_MAP = normal;
    ROUGHNESS = clamp(roughness, 0.0, 1.0);
    METALLIC = clamp(metallic, 0.0, 1.0);

    // Ensure fully opaque - this is critical
    ALPHA = 1.0;
    ALPHA_SCISSOR_THRESHOLD = 0.0;
}