// Recolours chosen cells of a KayKit palette texture (8 by 4 cells of gradients): a
// pixel in a chosen cell takes the new colour, scaled by how light the original pixel
// was against the cell's average, so the cell's shading is kept.
shader_type spatial;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;
uniform int cells[8];
uniform vec4 colors[8] : source_color;
uniform float references[8];
uniform int count = 0;

void fragment() {
	vec4 texel = texture(albedo_texture, UV);
	vec3 color = texel.rgb;
	int cell = int(floor(clamp(UV.y, 0.0, 0.9999) * 4.0)) * 8 + int(floor(clamp(UV.x, 0.0, 0.9999) * 8.0));

	for (int i = 0; i < count; i++) {
		if (cell == cells[i]) {
			float light = dot(texel.rgb, vec3(0.299, 0.587, 0.114));
			color = colors[i].rgb * clamp(light / max(references[i], 0.05), 0.35, 1.6);
		}
	}

	ALBEDO = color;
	ROUGHNESS = 0.8;
}
