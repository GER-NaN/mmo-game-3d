// Full-screen effects for the graphics panel (F9): drawn over the world, under the
// game's screens. Every effect is off at its default, so it can be judged one at a time.
shader_type canvas_item;

uniform sampler2D screen : hint_screen_texture, filter_nearest;

uniform bool pixelate = false;
uniform float pixel_size : hint_range(1.0, 32.0) = 4.0;
uniform bool posterize = false;
uniform float steps : hint_range(2.0, 32.0) = 6.0;
uniform float vignette : hint_range(0.0, 1.0) = 0.0;
uniform float grain : hint_range(0.0, 0.5) = 0.0;
uniform float fringe : hint_range(0.0, 8.0) = 0.0;
uniform float scanlines : hint_range(0.0, 1.0) = 0.0;
uniform float sharpen : hint_range(0.0, 2.0) = 0.0;
uniform vec4 tint : source_color = vec4(1.0);
uniform float tint_amount : hint_range(0.0, 1.0) = 0.0;

float noise(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	vec2 pixel = SCREEN_PIXEL_SIZE;
	vec2 uv = SCREEN_UV;

	if (pixelate) {
		vec2 cell = pixel * pixel_size;
		uv = (floor(uv / cell) + 0.5) * cell;
	}

	vec3 colour;

	// Red and blue drift apart towards the edges, by up to this many pixels.
	if (fringe > 0.0) {
		vec2 drift = (uv - 0.5) * 2.0 * fringe * pixel;
		colour.r = texture(screen, uv + drift).r;
		colour.g = texture(screen, uv).g;
		colour.b = texture(screen, uv - drift).b;
	} else {
		colour = texture(screen, uv).rgb;
	}

	if (sharpen > 0.0) {
		vec3 around = (texture(screen, uv + vec2(pixel.x, 0.0)).rgb
			+ texture(screen, uv - vec2(pixel.x, 0.0)).rgb
			+ texture(screen, uv + vec2(0.0, pixel.y)).rgb
			+ texture(screen, uv - vec2(0.0, pixel.y)).rgb) * 0.25;
		colour += (colour - around) * sharpen;
	}

	if (posterize) {
		colour = floor(colour * steps + 0.5) / steps;
	}

	colour = mix(colour, colour * tint.rgb, tint_amount);

	if (scanlines > 0.0) {
		colour *= 1.0 - scanlines * 0.5 * (0.5 + 0.5 * sin(FRAGCOORD.y * 3.14159));
	}

	if (grain > 0.0) {
		colour += (noise(FRAGCOORD.xy + vec2(TIME * 61.0, TIME * 17.0)) - 0.5) * grain;
	}

	if (vignette > 0.0) {
		colour *= 1.0 - vignette * smoothstep(0.3, 0.85, distance(SCREEN_UV, vec2(0.5)));
	}

	COLOR = vec4(colour, 1.0);
}
