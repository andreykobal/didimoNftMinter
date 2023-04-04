This package contains a didimo.

Inside you will find the core avatar file:

"avatar.gltf": your didimo in industry-standard glTF format.

Along with the following binary files:
"animations.bin": animation data
"ddmo_body.bin": body mesh
"ddmo_head.bin": head mesh
"ddmo_eyeRight.bin": right eye mesh
"ddmo_eyeLeft.bin": left eye mesh
"ddmo_eyelashes.bin": eyelashes mesh
"ddmo_mouth.bin": mouth mesh
"ddmo_clothing.bin": clothing mesh

And texture files:
"ddmo_head_albedo.jpg": albedo map texture covering the head and face
"ddmo_head_normal.jpg": normal map texture covering the head and face
"ddmo_mouth_albedo.jpg": albedo map texture covering the mouth
"ddmo_mouth_normal.jpg": normal map texture covering the mouth
"ddmo_eye_albedo.jpg": albedo map texture covering the eyes
"ddmo_eye_normal.jpg": normal map texture covering the eyes
"ddmo_eyelashes_albedo_opacity.png": albedo opacity map texture covering the eyelashes
"ddmo_body_albedo.jpg": albedo map texture covering the body
"ddmo_body_normal.jpg": normal map texture covering the body
"ddmo_body_clothing_mask.png": clothing mask covering the body
"ddmo_clothing_albedo_opacity.png": albedo opacity map texture covering the clothing
"ddmo_clothing_normal.jpg": normal map texture covering the clothing

Extras:
"deformation_matrix.dmx": to be used on deformation related endpoints
"metadata.json": includes metadata about the package
"avatar_info.json": companion file that specifies didimo meta information (texture maps, animation frames)


Instructions for the use of your didimo:
----------------------------------------

The provided glTF file includes the mesh geometry of the didimo, an attached facial skeleton, blendshapes, and keyframes for animation and posing.

To use your didimo inside a project or application, import the glTF file using an appropriate glTF importer.

Sometimes the textures may have to be assigned manually, according to the application.

Enjoy your didimo!


Website: https://didimo.co
Customer Portal: https://app.didimo.co
Developer Portal: https://docs.didimo.co
Email: support@didimo.co