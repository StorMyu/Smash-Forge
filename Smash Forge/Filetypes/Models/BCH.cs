using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;

namespace Smash_Forge
{
    /*
     * Everything in this class is adapted from gdkchan's Ohana3DS
     * This version will ONLY support Smash-style BCHs
     * Really WIP
     */
    public class BCH : FileBase
    {

        public override Endianness Endian { get; set; }

        public MBN mbn;
        public List<BCH_Model> models = new List<BCH_Model>();
        public Dictionary<string, BCH_Texture> textures = new Dictionary<string, BCH_Texture>();


        public override void Read(string filename)
        {


            bchHeader header = new bchHeader();
            FileData f = new FileData(filename);
            f.Endian = System.IO.Endianness.Little;

            f.skip(4);
            header.backwardCompatibility = f.readByte();
            header.forwardCompatibility = f.readByte();
            header.version = f.readShort();

            header.mainHeaderOffset = f.readInt();
            header.stringTableOffset = f.readInt();
            header.gpuCommandsOffset = f.readInt();
            header.dataOffset = f.readInt();
            if (header.backwardCompatibility > 0x20) header.dataExtendedOffset = f.readInt();
            header.relocationTableOffset = f.readInt();

            header.mainHeaderLength = f.readInt();
            header.stringTableLength = f.readInt();
            header.gpuCommandsLength = f.readInt();
            header.dataLength = f.readInt();
            if (header.backwardCompatibility > 0x20) header.dataExtendedLength = f.readInt();
            header.relocationTableLength = f.readInt();

            header.uninitializedDataSectionLength = f.readInt();
            header.uninitializedDescriptionSectionLength = f.readInt();

            if (header.backwardCompatibility > 7)
            {
                header.flags = f.readShort();
                header.addressCount = f.readShort();
            }

            // TODO: Finished Relocation table stuff
            for (int i = 0; i < header.relocationTableLength; i += 4)
            {
                f.seek(header.relocationTableOffset + i);
                int val = f.readInt();
                int off = val & 0x1FFFFFF;
                byte flag = (byte)(val >> 25);

                switch (flag)
                {
                    case 0:
                        f.seek((off * 4) + header.mainHeaderOffset);
                        f.writeInt((off * 4) + header.mainHeaderOffset, f.readInt() + header.mainHeaderOffset);
                        break;

                    case 1:
                        f.seek(off + header.mainHeaderOffset);
                        f.writeInt((off) + header.mainHeaderOffset, f.readInt() + header.stringTableOffset);
                        break;

                    case 2:
                        f.seek((off * 4) + header.mainHeaderOffset);
                        f.writeInt((off * 4) + header.mainHeaderOffset, f.readInt() + header.gpuCommandsOffset);
                        break;

                    case 0xc:
                        f.seek((off * 4) + header.mainHeaderOffset);
                        f.writeInt((off * 4) + header.mainHeaderOffset, f.readInt() + header.dataOffset);
                        break;
                }
                
                /*f.seek((off * 4) + header.gpuCommandsOffset);
                if (header.backwardCompatibility < 6)
                {
                    switch (flag)
                    {
                        case 0x23: f.writeInt((off * 4) + header.mainHeaderOffset, f.readInt() + header.dataOffset); break; //Texture
                        case 0x25: f.writeInt((off * 4) + header.mainHeaderOffset, f.readInt() + header.dataOffset); break; //Vertex
                        // Trying to understand the mess first
                        case 0x26: f.writeInt((off * 4) + header.mainHeaderOffset, Convert.ToInt32(((f.readInt() + header.dataOffset) & 0x7fffffff) | 0x80000000)); break; //Index 16 bits mode
                                                                                                                                                                             //case 0x27: writer.Write((peek(input) + header.dataOffset) & 0x7fffffff); break; //Index 8 bits mode
                    }
                }
                                else if (header.backwardCompatibility < 8)
                                {
                                    switch (flag)
                                    {
                                        case 0x24: writer.Write(peek(input) + header.dataOffset); break; //Texture
                                        case 0x26: writer.Write(peek(input) + header.dataOffset); break; //Vertex
                                        case 0x27: writer.Write(((peek(input) + header.dataOffset) & 0x7fffffff) | 0x80000000); break; //Index 16 bits mode
                                        case 0x28: writer.Write((peek(input) + header.dataOffset) & 0x7fffffff); break; //Index 8 bits mode
                                    }
                                }
                                else if (header.backwardCompatibility < 0x21)
                                {
                                    switch (flag)
                                    {
                                        case 0x25: writer.Write(peek(input) + header.dataOffset); break; //Texture
                                        case 0x27: writer.Write(peek(input) + header.dataOffset); break; //Vertex
                                        case 0x28: writer.Write(((peek(input) + header.dataOffset) & 0x7fffffff) | 0x80000000); break; //Index 16 bits mode
                                        case 0x29: writer.Write((peek(input) + header.dataOffset) & 0x7fffffff); break; //Index 8 bits mode
                                    }
                                }
                                else
                                {
                                    switch (flag)
                                    {
                                        case 0x25: writer.Write(peek(input) + header.dataOffset); break; //Texture
                                        case 0x26: writer.Write(peek(input) + header.dataOffset); break; //Vertex relative to Data Offset
                                        case 0x27: writer.Write(((peek(input) + header.dataOffset) & 0x7fffffff) | 0x80000000); break; //Index 16 bits mode relative to Data Offset
                                        case 0x28: writer.Write((peek(input) + header.dataOffset) & 0x7fffffff); break; //Index 8 bits mode relative to Data Offset
                                        case 0x2b: writer.Write(peek(input) + header.dataExtendedOffset); break; //Vertex relative to Data Extended Offset
                                        case 0x2c: writer.Write(((peek(input) + header.dataExtendedOffset) & 0x7fffffff) | 0x80000000); break; //Index 16 bits mode relative to Data Extended Offset
                                        case 0x2d: writer.Write((peek(input) + header.dataExtendedOffset) & 0x7fffffff); break; //Index 8 bits mode relative to Data Extended Offset
                                    }
                                }
                */
            }


            // Content Header
            f.seek(header.mainHeaderOffset);
            bchContentHeader content = new bchContentHeader();
            {
                content.modelsPointerTableOffset = f.readInt();
                content.modelsPointerTableEntries = f.readInt();
                content.modelsNameOffset = f.readInt();
                content.materialsPointerTableOffset = f.readInt();
                content.materialsPointerTableEntries = f.readInt();
                content.materialsNameOffset = f.readInt();
                content.shadersPointerTableOffset = f.readInt();
                content.shadersPointerTableEntries = f.readInt();
                content.shadersNameOffset = f.readInt();
                content.texturesPointerTableOffset = f.readInt();
                content.texturesPointerTableEntries = f.readInt();
                content.texturesNameOffset = f.readInt();
                content.materialsLUTPointerTableOffset = f.readInt();
                content.materialsLUTPointerTableEntries = f.readInt();
                content.materialsLUTNameOffset = f.readInt();
                content.lightsPointerTableOffset = f.readInt();
                content.lightsPointerTableEntries = f.readInt();
                content.lightsNameOffset = f.readInt();
                content.camerasPointerTableOffset = f.readInt();
                content.camerasPointerTableEntries = f.readInt();
                content.camerasNameOffset = f.readInt();
                content.fogsPointerTableOffset = f.readInt();
                content.fogsPointerTableEntries = f.readInt();
                content.fogsNameOffset = f.readInt();
                content.skeletalAnimationsPointerTableOffset = f.readInt();
                content.skeletalAnimationsPointerTableEntries = f.readInt();
                content.skeletalAnimationsNameOffset = f.readInt();
                content.materialAnimationsPointerTableOffset = f.readInt();
                content.materialAnimationsPointerTableEntries = f.readInt();
                content.materialAnimationsNameOffset = f.readInt();
                content.visibilityAnimationsPointerTableOffset = f.readInt();
                content.visibilityAnimationsPointerTableEntries = f.readInt();
                content.visibilityAnimationsNameOffset = f.readInt();
                content.lightAnimationsPointerTableOffset = f.readInt();
                content.lightAnimationsPointerTableEntries = f.readInt();
                content.lightAnimationsNameOffset = f.readInt();
                content.cameraAnimationsPointerTableOffset = f.readInt();
                content.cameraAnimationsPointerTableEntries = f.readInt();
                content.cameraAnimationsNameOffset = f.readInt();
                content.fogAnimationsPointerTableOffset = f.readInt();
                content.fogAnimationsPointerTableEntries = f.readInt();
                content.fogAnimationsNameOffset = f.readInt();
                content.scenePointerTableOffset = f.readInt();
                content.scenePointerTableEntries = f.readInt();
                content.sceneNameOffset = f.readInt();
            }

            //Shaders (unused for now, until someone wants to add them)
            for (int index = 0; index < content.shadersPointerTableEntries; index++)
            {
                f.seek(content.shadersPointerTableOffset + (index * 4));
                int dataOffset = f.readInt();
                f.seek(dataOffset);

                int shaderDataOffset = f.readInt();
                int shaderDataLength = f.readInt();
            }

            // Textures
            // WIP Section
            for (int index = 0; index < content.texturesPointerTableEntries; index++)
            {
                f.seek(content.texturesPointerTableOffset + (index * 4));
                int dOffset = f.readInt();
                f.seek(dOffset);
                Debug.WriteLine("dOffset: " + dOffset.ToString("X"));
                int textureCommandsOffset = f.readInt();
                int textureCommandsWordCount = f.readInt();

                f.seek(f.pos() + 0x14);
                String textureName = f.readString(f.readInt(), -1);
                //Debug.WriteLine("gpuCommandOffset: " + header.gpuCommandsOffset.ToString("X"));
                f.seek(textureCommandsOffset);
                //Debug.WriteLine("textureCommandOffset: " + textureCommandsOffset.ToString("X"));
                BCH_Texture tex = new BCH_Texture();
                textures.Add(textureName, tex);

                tex.height = f.readShort();
                tex.width = f.readShort();
                f.skip(12);
                int doffset = f.readInt();
                //Debug.WriteLine("doffset: " + doffset.ToString("X"));
                f.skip(4);
                tex.type = f.readInt();
                tex.data = f.getSection(doffset, f.size() - doffset);

                if (tex.type == 12)
                    tex.display = NUT.loadImage(Pixel.decodeETC(tex.data, tex.width, tex.height));
            }

            // Model data
            
            for (int modelIndex = 0; modelIndex < content.modelsPointerTableEntries; modelIndex++)
            {
                f.seek(content.modelsPointerTableOffset + (modelIndex * 4));
                int objectsHeaderOffset = f.readInt();

                // Objects
                f.seek(objectsHeaderOffset);
                BCH_Model model = new BCH_Model();
                models.Add(model);

                model.flags = f.readByte();
                model.skeletonScaleType = f.readByte();
                model.silhouetteMaterialEntries = f.readShort();

                model.worldTransform = new Matrix4(f.readFloat(), f.readFloat(), f.readFloat(), f.readFloat()
                    , f.readFloat(), f.readFloat(), f.readFloat(), f.readFloat()
                    , f.readFloat(), f.readFloat(), f.readFloat(), f.readFloat()
                    , 0, 0, 0, 1);

                model.materialsTableOffset = f.readInt();
                model.materialsTableEntries = f.readInt();
                model.materialsNameOffset = f.readInt();
                model.verticesTableOffset = f.readInt();
                //Debug.WriteLine("Mesh Count: " + f.pos().ToString("X"));
                model.verticesTableEntries = f.readInt();
                f.skip(0x28);
                model.skeletonOffset = f.readInt();
                model.skeletonEntries = f.readInt();
                model.skeletonNameOffset = f.readInt();
                model.objectsNodeVisibilityOffset = f.readInt();
                model.objectsNodeCount = f.readInt();
                model.name = f.readString(f.readInt(), -1);
                model.objectsNodeNameEntries = f.readInt();
                model.objectsNodeNameOffset = f.readInt();
                f.readInt(); //0x0
                model.metaDataPointerOffset = f.readInt();

                f.seek(model.objectsNodeVisibilityOffset);
                int nodeVisibility = f.readInt();

                string[] objectName = new string[model.objectsNodeNameEntries];
                f.seek(model.objectsNodeNameOffset);
                int rootReferenceBit = f.readInt(); //Radix tree
                int rootLeftNode = f.readShort();
                int rootRightNode = f.readShort();
                int rootNameOffset = f.readInt() + header.mainHeaderOffset;

                for (int i = 0; i < model.objectsNodeNameEntries; i++)
                {
                    int referenceBit = f.readInt();
                    short leftNode = (short)f.readShort();
                    short rightNode = (short)f.readShort();
                    objectName[i] = f.readString(f.readInt(), -1);
                    //Debug.WriteLine(objectName[i]);
                }

                // Materials
                // NOTE: MATERIALS AND OBJECT SECTIONS ARE REALLY MESSY ATM

                String[] materialNames = new String[model.materialsTableEntries];
                for (int index = 0; index < model.materialsTableEntries; index++)
                {
                    f.seek(model.materialsTableOffset + (index * 0x2c));

                    int materialParametersOffset = f.readInt();
                    f.readInt();
                    f.readInt();
                    f.readInt();
                    int textureCommandsOffset = f.readInt();
                    int textureCommandsWordCount = f.readInt();

                    int materialMapperOffset = f.readInt();
                    materialNames[index] = f.readString(f.readInt(), -1);
                }

                // Object Descriptions...
                // Assumes MBN is already loaded for now
                f.seek(model.verticesTableOffset);
                List<objDes> objDescriptors = new List<objDes>();
                Debug.WriteLine(model.name);
                if (mbn == null)
                {
                    mbn = new Smash_Forge.MBN();
                    for (int index = 0; index < model.verticesTableEntries; index++)
                        mbn.mesh.Add(new MBN.Mesh());
                    mbn.PreRender();
                }
                for (int index = 0; index < mbn.mesh.Count; index++)
                {
                    int i = f.readShort();
                    if (index > mbn.mesh.Count) break;
                    if (i > materialNames.Length) break;
                    mbn.mesh[index].texId = textures[materialNames[i]].display;
                    Console.WriteLine("Tex index" + mbn.mesh[index].texId);
                    f.skip(2); // flags
                    int nameId = f.readShort();
                    mbn.mesh[index].name = objectName[nameId];

                    // node visibility TODO: finish...
                    //mbn.mesh[index].isVisible = ((nodeVisibility & (1 << nameId)) > 0);

                    mbn.mesh[index].renderPriority = f.readShort();

                    objDes des = new objDes();
                    objDescriptors.Add(des);
                    des.vshAttBufferCommandOffset = f.readInt();
                    des.vshAttBufferCommandCount = f.readInt();
                    des.faceOffset = f.readInt();
                    des.faceCount = f.readInt();
                    des.vshAttBufferCommandOffsetEx = f.readInt();
                    des.vshAttBufferCommandCountEx = f.readInt();

                    f.skip(12);// center vector
                    f.skip(4); // flagsOffset
                    f.skip(4); // 0?
                    f.readInt(); //bbOffsets[i] =  + mainheaderOffset

                    //Debug.WriteLine(des.vshAttBufferCommandOffset.ToString("X"));
                }


                //Skeleton
                f.seek(model.skeletonOffset);
                for (int index = 0; index < model.skeletonEntries; index++)
                {
                    Bone bone = new Smash_Forge.Bone(model.skeleton);
                    int boneFlags = f.readInt();
                    bone.parentIndex = (short)f.readShort();
                    short boneSpace = (short)f.readShort();
                    bone.scale = new float[3];
                    bone.rotation = new float[3];
                    bone.position = new float[3];
                    bone.scale[0] = f.readFloat();
                    bone.scale[1] = f.readFloat();
                    bone.scale[2] = f.readFloat();
                    bone.rotation[0] = f.readFloat();
                    bone.rotation[1] = f.readFloat();
                    bone.rotation[2] = f.readFloat();
                    bone.position[0] = f.readFloat();
                    bone.position[1] = f.readFloat();
                    bone.position[2] = f.readFloat();

                    // bone matrix... not really needed to be stored per say
                    f.skip(4 * 4 * 3);

                    bone.boneName = f.readString(f.readInt(), -1).ToCharArray();

                    f.skip(4); // Meta data

                    model.skeleton.bones.Add(bone);
                }
                model.skeleton.reset();

/*                //Skeletal Animations

                SkelAnimation anim = new SkelAnimation();

                for (int index = 0; index < content.skeletalAnimationsPointerTableEntries; index++)
                 {
                    f.seek(content.skeletalAnimationsPointerTableOffset + (index * 4));
                    int dataOffset = f.readInt();
                    f.seek(dataOffset);

                    RenderBase.OSkeletalAnimation skeletalAnimation = new RenderBase.OSkeletalAnimation();

                    skeletalAnimation.name = f.readString(f.readInt(), -1);
                    int animationFlags = f.readInt();
                    skeletalAnimation.loopMode = (RenderBase.OLoopMode)(animationFlags & 1);
                    skeletalAnimation.frameSize = f.readFloat();
                    int boneTableOffset = f.readInt();
                    int boneTableEntries = f.readInt();
                    int metaDataPointerOffset = f.readInt();

//                    /*if (metaDataPointerOffset != 0)
//                    {
//                        data.Seek(metaDataPointerOffset, SeekOrigin.Begin);
//                        skeletalAnimation.userData = getMetaData(input);
//                    }

                    for (int i = 0; i < boneTableEntries; i++)
                    {
                        f.seek(boneTableOffset + (i * 4));
                        int offset = f.readInt();

                        RenderBase.OSkeletalAnimationBone bone = new RenderBase.OSkeletalAnimationBone();

                        f.seek(offset);
                        bone.name = f.readString(f.readInt(), -1);
                        int animationTypeFlags = f.readInt();

                        int flags = f.readInt();

                        RenderBase.OSegmentType segmentType = (RenderBase.OSegmentType)((animationTypeFlags >> 16) & 0xf);
                                        switch (segmentType)
                                        {
                            case RenderBase.OSegmentType.transform:
                                f.seek(offset + 0x18);

                                uint notExistMask = 0x80000;
                                uint constantMask = 0x200;

                                for (int j = 0; j < 2; j++)
                                {
                                    for (int axis = 0; axis < 3; axis++)
                                    {
                                        bool notExist = (flags & notExistMask) > 0;
                                        bool constant = (flags & constantMask) > 0;

                                        RenderBase.OAnimationKeyFrameGroup frame = new RenderBase.OAnimationKeyFrameGroup();
                                        frame.exists = !notExist;
                                        if (frame.exists)
                                        {
                                            if (constant)
                                            {
                                                frame.interpolation = RenderBase.OInterpolationMode.linear;
                                                frame.keyFrames.Add(new RenderBase.OAnimationKeyFrame(input.ReadSingle(), 0));
                                            }
                                            else
                                            {
                                                int frameOffset = f.readInt();
                                                long position = data.Position;
                                                f.seek(frameOffset);
                                                getAnimationKeyFrame(input, frame);
                                                f.seek(position);
                                            }
                                        }
                                        else
                                            data.Seek(4, SeekOrigin.Current);

                                        if (j == 0)
                                        {
                                            switch (axis)
                                            {
                                                case 0: bone.rotationX = frame; break;
                                                case 1: bone.rotationY = frame; break;
                                                case 2: bone.rotationZ = frame; break;
                                            }
                                        }
                                        else
                                        {
                                            switch (axis)
                                            {
                                                case 0: bone.translationX = frame; break;
                                                case 1: bone.translationY = frame; break;
                                                case 2: bone.translationZ = frame; break;
                                            }
                                        }

                                        notExistMask <<= 1;
                                        constantMask <<= 1;
                                    }

                                    constantMask <<= 1;
                                }

                                break;
                            case RenderBase.OSegmentType.transformQuaternion:
                                bone.isFrameFormat = true;

                                uint scaleOffset = input.ReadUInt32();
                                uint rotationOffset = input.ReadUInt32();
                                uint translationOffset = input.ReadUInt32();

                                if ((flags & 0x20) == 0)
                                {
                                    bone.scale.exists = true;
                                    data.Seek(scaleOffset, SeekOrigin.Begin);

                                    if ((flags & 4) > 0)
                                    {
                                        bone.scale.vector.Add(new RenderBase.OVector4(
                                            input.ReadSingle(),
                                            input.ReadSingle(),
                                            input.ReadSingle(),
                                            0));
                                    }
                                    else
                                    {
                                        bone.scale.startFrame = input.ReadSingle();
                                        bone.scale.endFrame = input.ReadSingle();

                                        uint scaleFlags = input.ReadUInt32();
                                        uint scaleDataOffset = input.ReadUInt32();
                                        uint scaleEntries = input.ReadUInt32();

                                        data.Seek(scaleDataOffset, SeekOrigin.Begin);
                                        for (int j = 0; j < scaleEntries; j++)
                                        {
                                            bone.scale.vector.Add(new RenderBase.OVector4(
                                                input.ReadSingle(),
                                                input.ReadSingle(),
                                                input.ReadSingle(),
                                                0));
                                        }
                                    }
                                }

                                if ((flags & 0x10) == 0)
                                                {
                                                    bone.rotationQuaternion.exists = true;
                                                    data.Seek(rotationOffset, SeekOrigin.Begin);

                                                    if ((flags & 2) > 0)
                                                    {
                                                        bone.rotationQuaternion.vector.Add(new RenderBase.OVector4(
                                                            input.ReadSingle(),
                                                            input.ReadSingle(),
                                                            input.ReadSingle(),
                                                            input.ReadSingle()));
                                                    }
                                                    else
                                                    {
                                                        bone.rotationQuaternion.startFrame = input.ReadSingle();
                                                        bone.rotationQuaternion.endFrame = input.ReadSingle();

                                                        uint rotationFlags = input.ReadUInt32();
                                                        uint rotationDataOffset = input.ReadUInt32();
                                                        uint rotationEntries = input.ReadUInt32();

                                                        data.Seek(rotationDataOffset, SeekOrigin.Begin);
                                                        for (int j = 0; j < rotationEntries; j++)
                                                        {
                                                            bone.rotationQuaternion.vector.Add(new RenderBase.OVector4(
                                                                input.ReadSingle(),
                                                                input.ReadSingle(),
                                                                input.ReadSingle(),
                                                                input.ReadSingle()));
                                                        }
                                                    }
                                                }

                                                if ((flags & 8) == 0)
                                                {
                                                    bone.translation.exists = true;
                                                    data.Seek(translationOffset, SeekOrigin.Begin);

                                                    if ((flags & 1) > 0)
                                                    {
                                                        bone.translation.vector.Add(new RenderBase.OVector4(
                                                            input.ReadSingle(),
                                                            input.ReadSingle(),
                                                            input.ReadSingle(),
                                                            0));
                                                    }
                                                    else
                                                    {
                                                        bone.translation.startFrame = input.ReadSingle();
                                                        bone.translation.endFrame = input.ReadSingle();

                                                        uint translationFlags = input.ReadUInt32();
                                                        uint translationDataOffset = input.ReadUInt32();
                                                        uint translationEntries = input.ReadUInt32();

                                                        data.Seek(translationDataOffset, SeekOrigin.Begin);
                                                        for (int j = 0; j < translationEntries; j++)
                                                        {
                                                            bone.translation.vector.Add(new RenderBase.OVector4(
                                                                input.ReadSingle(),
                                                                input.ReadSingle(),
                                                                input.ReadSingle(),
                                                                0));
                                                        }
                                                    }
                                                }

                                                break;
                                            case RenderBase.OSegmentType.transformMatrix:
                                                bone.isFullBakedFormat = true;

                                                input.ReadUInt32();
                                                input.ReadUInt32();
                                                uint matrixOffset = input.ReadUInt32();
                                                uint entries = input.ReadUInt32();

                                                data.Seek(matrixOffset, SeekOrigin.Begin);
                                                for (int j = 0; j < entries; j++)
                                                {
                                                    RenderBase.OMatrix transform = new RenderBase.OMatrix();
                                                    transform.M11 = input.ReadSingle();
                                                    transform.M21 = input.ReadSingle();
                                                    transform.M31 = input.ReadSingle();
                                                    transform.M41 = input.ReadSingle();

                                                    transform.M12 = input.ReadSingle();
                                                    transform.M22 = input.ReadSingle();
                                                    transform.M32 = input.ReadSingle();
                                                    transform.M42 = input.ReadSingle();

                                                    transform.M13 = input.ReadSingle();
                                                    transform.M23 = input.ReadSingle();
                                                    transform.M33 = input.ReadSingle();
                                                    transform.M43 = input.ReadSingle();

                                                    bone.transform.Add(transform);
                                                }

                                                break;
                                            default: throw new Exception(string.Format("BCH: Unknow Segment Type {0} on Skeletal Animation bone {1}! STOP!", segmentType, bone.name));
                                        }

                                        skeletalAnimation.bone.Add(bone);
                                    }

                                    models.skeletalAnimation.list.Add(skeletalAnimation);
                                }

                                //Material Animations
                                for (int index = 0; index < contentHeader.materialAnimationsPointerTableEntries; index++)
                                {
                                    data.Seek(contentHeader.materialAnimationsPointerTableOffset + (index * 4), SeekOrigin.Begin);
                                    uint dataOffset = input.ReadUInt32();
                                    data.Seek(dataOffset, SeekOrigin.Begin);

                                    RenderBase.OMaterialAnimation materialAnimation = new RenderBase.OMaterialAnimation();

                                    materialAnimation.name = readString(input);
                                    uint animationFlags = input.ReadUInt32();
                                    materialAnimation.loopMode = (RenderBase.OLoopMode)(animationFlags & 1);
                                    materialAnimation.frameSize = input.ReadSingle();
                                    uint dataTableOffset = input.ReadUInt32();
                                    uint dataTableEntries = input.ReadUInt32();
                                    input.ReadUInt32();
                                    uint textureNameTableOffset = input.ReadUInt32();
                                    uint textureNameTableEntries = input.ReadUInt32();

                                    data.Seek(textureNameTableOffset, SeekOrigin.Begin);
                                    for (int i = 0; i < textureNameTableEntries; i++)
                                    {
                                        string name = readString(input);
                                        materialAnimation.textureName.Add(name);
                                    }

                                    for (int i = 0; i < dataTableEntries; i++)
                                    {
                                        data.Seek(dataTableOffset + (i * 4), SeekOrigin.Begin);
                                        uint offset = input.ReadUInt32();

                                        RenderBase.OMaterialAnimationData animationData = new RenderBase.OMaterialAnimationData();

                                        data.Seek(offset, SeekOrigin.Begin);
                                        animationData.name = readString(input);
                                        uint animationTypeFlags = input.ReadUInt32();
                                        uint flags = input.ReadUInt32();

                                        animationData.type = (RenderBase.OMaterialAnimationType)(animationTypeFlags & 0xff);
                                        RenderBase.OSegmentType segmentType = (RenderBase.OSegmentType)((animationTypeFlags >> 16) & 0xf);

                                        int segmentCount = 0;
                                        switch (segmentType)
                                        {
                                            case RenderBase.OSegmentType.rgbaColor: segmentCount = 4; break;
                                            case RenderBase.OSegmentType.vector2: segmentCount = 2; break;
                                            case RenderBase.OSegmentType.single: segmentCount = 1; break;
                                            case RenderBase.OSegmentType.integer: segmentCount = 1; break;
                                        }

                                        for (int j = 0; j < segmentCount; j++)
                                        {
                                            RenderBase.OAnimationKeyFrameGroup frame = new RenderBase.OAnimationKeyFrameGroup();

                                            data.Seek(offset + 0xc + (j * 4), SeekOrigin.Begin);

                                            frame.exists = (flags & (0x100 << j)) == 0;
                                            bool constant = (flags & (1 << j)) > 0;

                                            if (frame.exists)
                                            {
                                                if (constant)
                                                {
                                                    frame.interpolation = RenderBase.OInterpolationMode.linear;
                                                    frame.keyFrames.Add(new RenderBase.OAnimationKeyFrame(input.ReadSingle(), 0));
                                                }
                                                else
                                                {
                                                    uint frameOffset = input.ReadUInt32();
                                                    data.Seek(frameOffset, SeekOrigin.Begin);
                                                    getAnimationKeyFrame(input, frame);
                                                }
                                            }

                                            animationData.frameList.Add(frame);
                                        }

                                        materialAnimation.data.Add(animationData);
                                    }

                                    models.materialAnimation.list.Add(materialAnimation);
                                }
                                
          */

            }
        }

        public override byte[] Rebuild()
        {
            throw new NotImplementedException();
        }


        #region helpers for reading

        public struct objDes // bchObjectEntry 
        {
            public short materialId;
            public bool isSilhouette;
            public short nodeId;
            public short renderPriority;
            public int vshAttBufferCommandOffset; // vshAttributesBufferCommandsOffset;
            public int vshAttBufferCommandCount;// vshAttributesBufferCommandsWordCount;
            public int faceOffset; // facesHeaderOffset;
            public int faceCount; // facesHeaderEntries;
            public int vshAttBufferCommandOffsetEx; // vshExtraAttributesBufferCommandsOffset;
            public int vshAttBufferCommandCountEx; // vshExtraAttributesBufferCommandsWordCount;
            public Vector3 centerVector;
            public int flagsOffset;
            public int boundingBoxOffset;
        }

        public struct bchHeader
        {
            public string magic;
            public int backwardCompatibility;
            public int forwardCompatibility;
            public int version;

            public int mainHeaderOffset;
            public int stringTableOffset;
            public int gpuCommandsOffset;
            public int dataOffset;
            public int dataExtendedOffset;
            public int relocationTableOffset;

            public int mainHeaderLength;
            public int stringTableLength;
            public int gpuCommandsLength;
            public int dataLength;
            public int dataExtendedLength;
            public int relocationTableLength;
            public int uninitializedDataSectionLength;
            public int uninitializedDescriptionSectionLength;

            public int flags;
            public int addressCount;
        }

        private struct bchContentHeader
        {
            public int modelsPointerTableOffset;
            public int modelsPointerTableEntries;
            public int modelsNameOffset;
            public int materialsPointerTableOffset;
            public int materialsPointerTableEntries;
            public int materialsNameOffset;
            public int shadersPointerTableOffset;
            public int shadersPointerTableEntries;
            public int shadersNameOffset;
            public int texturesPointerTableOffset;
            public int texturesPointerTableEntries;
            public int texturesNameOffset;
            public int materialsLUTPointerTableOffset;
            public int materialsLUTPointerTableEntries;
            public int materialsLUTNameOffset;
            public int lightsPointerTableOffset;
            public int lightsPointerTableEntries;
            public int lightsNameOffset;
            public int camerasPointerTableOffset;
            public int camerasPointerTableEntries;
            public int camerasNameOffset;
            public int fogsPointerTableOffset;
            public int fogsPointerTableEntries;
            public int fogsNameOffset;
            public int skeletalAnimationsPointerTableOffset;
            public int skeletalAnimationsPointerTableEntries;
            public int skeletalAnimationsNameOffset;
            public int materialAnimationsPointerTableOffset;
            public int materialAnimationsPointerTableEntries;
            public int materialAnimationsNameOffset;
            public int visibilityAnimationsPointerTableOffset;
            public int visibilityAnimationsPointerTableEntries;
            public int visibilityAnimationsNameOffset;
            public int lightAnimationsPointerTableOffset;
            public int lightAnimationsPointerTableEntries;
            public int lightAnimationsNameOffset;
            public int cameraAnimationsPointerTableOffset;
            public int cameraAnimationsPointerTableEntries;
            public int cameraAnimationsNameOffset;
            public int fogAnimationsPointerTableOffset;
            public int fogAnimationsPointerTableEntries;
            public int fogAnimationsNameOffset;
            public int scenePointerTableOffset;
            public int scenePointerTableEntries;
            public int sceneNameOffset;
        }

        #endregion


        public class BCH_Texture
        {
            public int width, height, type;
            public byte[] data;
            // for display only
            public int display = 0;
        }

        public class BCH_Model
        {
            public int flags;
            public int skeletonScaleType;
            public int silhouetteMaterialEntries;

            public VBN skeleton = new VBN();

            public Matrix4 worldTransform;

            public int materialsTableOffset;
            public int materialsTableEntries;
            public int materialsNameOffset;
            public int verticesTableOffset;
            public int verticesTableEntries;
            public int skeletonOffset;
            public int skeletonEntries;
            public int skeletonNameOffset;
            public int objectsNodeVisibilityOffset;
            public int objectsNodeCount;
            public string name; //model name
            public int objectsNodeNameEntries;
            public int objectsNodeNameOffset;
            public int metaDataPointerOffset;
        }
    }
}