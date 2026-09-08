bl_info = {
    "name": "Celeste 3D Engine Exports",
    "author": "Eylstar",
    "version": (1, 0),
    "blender": (4, 0, 0),
    "location": "View3D > Sidebar > Celeste3DEngine",
    "description": "Exports object placements or animations as JSON for Celeste3DEngine",
    "category": "Import-Export",
}

import os
import bpy
import json
import mathutils


# MATRIX UTILS

BLENDER_TO_ENGINE = mathutils.Matrix((
    (1, 0, 0, 0),
    (0, 0, 1, 0),
    (0, -1, 0, 0),
    (0, 0, 0, 1),
))

def convert_matrix(mat):
    return BLENDER_TO_ENGINE @ mat @ BLENDER_TO_ENGINE.inverted()
    
    
    
    
# ANIMATION ACTION GETTER (FIRST NLA TRACK FALLBACK)

def get_action_and_range(obj):
    if obj.animation_data is None:
        return None, None
        
    if obj.animation_data.action is not None:
        action = obj.animation_data.action
        return action, action.frame_range
        
    for track in obj.animation_data.nla_tracks:
        for strip in track.strips:
            if strip.action is not None:
                return strip.action, (strip.frame_start, strip.frame_end)
                
    return None, None
        
        
# ANIMATION DATA EXPORT
        
def export_animation(obj, fps, loop, output_path):
    action, frame_range = get_action_and_range(obj)
    
    if action is None:
        return None, "Selected object has no animation data (no active action or NLA strip found)"
        
    scene = bpy.context.scene
    frame_start, frame_end = int(frame_range[0]), int(frame_range[1])
    
    original_frame = scene.frame_current
    
    pos_keys, rot_keys, scale_keys = [], [], []
    
    for frame in range(frame_start, frame_end + 1):
        scene.frame_set(frame)
        
        mat = convert_matrix(obj.matrix_basis)
        loc, rot, scl = mat.decompose()
        time = (frame - frame_start) / scene.render.fps
        
        pos_keys.append({"time": time, "value": [loc.x, loc.y, loc.z]})
        rot_keys.append({"time": time, "value": [rot.x, rot.y, rot.z, rot.w]})
        scale_keys.append({"time": time, "value": [scl.x, scl.y, scl.z]})
        
    scene.frame_set(original_frame)
    
    data = {
        "name": action.name,
        "duration": (frame_end - frame_start) / scene.render.fps,
        "loop": loop,
        "positionKeyframes": pos_keys,
        "rotationKeyframes": rot_keys,
        "scaleKeyframes" : scale_keys
    }
    
    with open(output_path, "w") as f:
        json.dump(data, f, indent=2)
        
    return len(pos_keys), None
    
    
# ANIMAITON PANEL

class C3D_OT_ExportAnimation(bpy.types.Operator):
    bl_idname = "c3d.export_animation"
    bl_label = "Export Animation"
    bl_description = "Export the selected object's animation as Transform keyframes"

    def execute(self, context):
        props = context.scene.c3d_anim_export_props

        if props.target_object is None:
            self.report({'ERROR'}, "No object selected")
            return {'CANCELLED'}

        if not props.output_path:
            self.report({'ERROR'}, "No output path set")
            return {'CANCELLED'}

        file_name = props.output_file_name.strip() if props.output_file_name else "animation"
        if not file_name.endswith(".json"):
            file_name += ".json"

        folder = bpy.path.abspath(props.output_path)
        os.makedirs(folder, exist_ok=True)
        output_path = os.path.join(folder, file_name)

        count, error = export_animation(props.target_object, context.scene.render.fps, props.loop, output_path)

        if error:
            self.report({'ERROR'}, error)
            return {'CANCELLED'}

        self.report({'INFO'}, f"Exported {count} keyframes to {output_path}")
        return {'FINISHED'}



class C3D_OT_ExportPlacements(bpy.types.Operator):
    bl_idname = "c3d.export_placements"
    bl_label = "Export Placements"
    bl_description = "Export the selected collection's transforms as JSON"

    def execute(self, context):
        props = context.scene.c3d_export_props
        
        if props.collection is None:
            self.report({'ERROR'}, "No collection selected")
            return {'CANCELLED'}

        if not props.output_path:
            self.report({'ERROR'}, "No output path set")
            return {'CANCELLED'}
            
        file_name = props.output_file_name.strip() if props.output_file_name else "placements"
        
        if not file_name.endswith(".json"):
            file_name += ".json"
            
        folder = bpy.path.abspath(props.output_path)
        os.makedirs(folder, exist_ok=True)

        output_path = os.path.join(folder, file_name)
        
        placements = []
        for obj in props.collection.objects:
            if obj.type != 'MESH':
                continue

            converted = convert_matrix(obj.matrix_world)
            loc, rot, scale = converted.decompose()

            placements.append({
                "model": obj.name.split(".")[0],
                "position": [loc.x, loc.y, loc.z],
                "rotation": [rot.x, rot.y, rot.z, rot.w],
                "scale": [scale.x, scale.y, scale.z]
            })

        with open(output_path, "w") as f:
            json.dump(placements, f, indent=2)

        self.report({'INFO'}, f"Exported {len(placements)} placements")
        return {'FINISHED'}


# EXPORT PANELS

class C3D_PT_ExportPanel(bpy.types.Panel):
    bl_label = "C3DEngine Placements Export"
    bl_idname = "C3D_PT_export_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "C3DEngine"

    def draw(self, context):	
        layout = self.layout
        props = context.scene.c3d_export_props

        layout.prop(props, "collection")
        layout.prop(props, "output_path")
        layout.prop(props, "output_file_name")
        layout.operator("c3d.export_placements")
        

class C3D_PT_AnimationExportPanel(bpy.types.Panel):
    bl_label = "C3DEngine Animation Export"
    bl_idname = "C3D_PT_animation_export_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "C3DEngine"

    def draw(self, context):
        layout = self.layout
        props = context.scene.c3d_anim_export_props

        layout.prop(props, "target_object")
        layout.prop(props, "loop")
        layout.prop(props, "output_path")
        layout.prop(props, "output_file_name")
        layout.operator("c3d.export_animation")
    



# EXPORT PROPERTIES

class C3D_ExportProperties(bpy.types.PropertyGroup):
    collection: bpy.props.PointerProperty(
        name="Collection",
        type=bpy.types.Collection
    )
    output_path: bpy.props.StringProperty(
        name="Output Folder",
        subtype='DIR_PATH'
    )
    output_file_name: bpy.props.StringProperty(
            name="File Name"
    )
    
    
class C3D_AnimExportProperties(bpy.types.PropertyGroup):
    target_object: bpy.props.PointerProperty(
        name="Object",
        type=bpy.types.Object
    )
    loop: bpy.props.BoolProperty(
        name="Loop",
        default=True
    )
    output_path: bpy.props.StringProperty(
        name="Output Folder",
        subtype='DIR_PATH'
    )
    output_file_name: bpy.props.StringProperty(
        name="File Name"
    )




classes = (
    C3D_OT_ExportPlacements, C3D_PT_ExportPanel, C3D_ExportProperties,
    C3D_OT_ExportAnimation, C3D_PT_AnimationExportPanel, C3D_AnimExportProperties,
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.c3d_export_props = bpy.props.PointerProperty(type=C3D_ExportProperties)
    bpy.types.Scene.c3d_anim_export_props = bpy.props.PointerProperty(type=C3D_AnimExportProperties)

def unregister():
    for cls in classes:
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.c3d_export_props
    del bpy.types.Scene.c3d_anim_export_props

if __name__ == "__main__":
    register()