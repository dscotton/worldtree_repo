#version 330

in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
out vec4 finalColor;

// Replaces each pixel's RGB with the draw tint while preserving the texture's
// alpha. Drawing with Color.White produces a sprite-shaped white silhouette.
void main() {
    vec4 texColor = texture(texture0, fragTexCoord);
    finalColor = vec4(fragColor.rgb, texColor.a);
}
