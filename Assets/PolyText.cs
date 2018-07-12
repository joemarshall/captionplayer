﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PolyText : Graphic {
    [TextArea]
    public string text;
    public TextAnchor alignment;
    public Font font;
    public int fontSize=64;
    public TextGenerator generator;
    public List<Vector2> clipPoly=new List<Vector2>();
    
    // two lists of positions - in centered stuff, we need one for odd numbers of lines, one for even number of lines
    // nb. note that  numbers of lines is a bit of a misnomer, as numbers of lines may change based on shape of polygon
    
    List<Rect> slices=new List<Rect>();
    List<Rect> offsetSlices=new List<Rect>();
    
    
    override protected void  Start()
    {
        base.Start();
        if(clipPoly.Count==0)
        {
            clipPoly.Add(new Vector2(0.4f,0.1f));
            clipPoly.Add(new Vector2(0.5f,0.1f));
            clipPoly.Add(new Vector2(.8f,.8f));
            clipPoly.Add(new Vector2(0.1f,.8f));
        }
    }

    void getPolyWidth(float y,out float left,out float right)
    {
        float minPos=1;
        float maxPos=0;
        float lastX=clipPoly[clipPoly.Count-1].x;
        float lastY=clipPoly[clipPoly.Count-1].y;
        for(int c=0;c<clipPoly.Count;c++)
        {
            float thisX=clipPoly[c].x;
            float thisY=clipPoly[c].y;
            if((thisY<=y && lastY>=y)|| (thisY>=y && lastY<=y))
            {
                // this line crosses past us
                // find the min and max points where lines intersect with us
                if(thisY==lastY)
                {
                    // if it is a straight horizontal line min and max points are left and right of that line respectively
                    maxPos=Mathf.Max(lastX,maxPos);
                    maxPos=Mathf.Max(thisX,maxPos);
                    minPos=Mathf.Min(lastX,minPos);
                    minPos=Mathf.Min(thisX,minPos);
                }else
                {
                    // we are somewhere on this line
                    // update max and min accordingly
                    float dx=thisX-lastX;
                    float dy=thisY-lastY;
                    float offsetY=thisY-y;
                    float offsetX=dx*(offsetY/dy);
                    float intersectX=thisX-offsetX;
                    minPos=Mathf.Min(intersectX,minPos);                    
                    maxPos=Mathf.Max(intersectX,maxPos);
//                    print("Intersection: "+thisX+","+thisY+","+lastX+","+lastY+"["+y+"]"+intersectX);
                }                
            }
            lastX=thisX;
            lastY=thisY;
        }
        left=minPos;
        right=maxPos;
    }

    
    void CalculateSlices(float lineHeight)
    {
        slices.Clear();
        offsetSlices.Clear();
        // find max and min of polygon
        
        float maxY=0;
        float minY=clipPoly[0].y;
        
        for(int c=0;c<clipPoly.Count;c++)
        {
            if(clipPoly[c].y>maxY)
            {
                maxY=clipPoly[c].y;
            }
            if(clipPoly[c].y<minY)
            {
                minY=clipPoly[c].y;
            }
        }
        
        
        // put the lines equally between min and max        
        int lineCount= (int)((maxY-minY)/lineHeight);
        float totalHeight=lineHeight*(float)lineCount;
        float startY=minY + 0.5f*((maxY-minY)-totalHeight);
        
        
        for(int c=0;c<lineCount;c++)
        {
            float endY=startY+lineHeight;
            float left0,right0;
            float left1,right1;
            getPolyWidth(startY,out left0,out right0);
            getPolyWidth(endY,out left1,out right1);
            float sliceLeft=Mathf.Max(left0,left1);
            float sliceRight=Mathf.Min(right0,right1);
            //print("Slice("+c+")"+sliceLeft+","+sliceRight);
            slices.Add(new Rect(sliceLeft,startY,sliceRight-sliceLeft,endY-startY));
            
            // for vertical centering, we need to have offset lines (for odd number of lines)
            if(c<lineCount-1)
            {
                float offsetStartY=startY+lineHeight*.5f;
                float offsetEndY=endY+lineHeight*0.5f;
                getPolyWidth(offsetStartY,out left0,out right0);
                getPolyWidth(offsetEndY,out left1,out right1);
                float oSliceLeft=Mathf.Max(left0,left1);
                float oSliceRight=Mathf.Min(right0,right1);
                offsetSlices.Add(new Rect(oSliceLeft,offsetStartY,oSliceRight-oSliceLeft,offsetEndY-offsetStartY));
            }
            
            startY=endY; 
        }
        
        
    }
    
    protected override void UpdateMaterial()
    {
        GetComponent<CanvasRenderer>().SetMaterial(font.material, null);
    }
    
    string reverseString(string input)
    {
        char[] array=input.ToCharArray();
        Array.Reverse(array);
        return new string(array);
    }
    

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        TextAnchor horzAlign=TextAnchor.UpperCenter;
        if(alignment==TextAnchor.MiddleCenter || alignment==TextAnchor.LowerCenter)horzAlign=TextAnchor.UpperCenter;
        if(alignment==TextAnchor.MiddleLeft || alignment==TextAnchor.LowerLeft || alignment==TextAnchor.UpperLeft)horzAlign=TextAnchor.UpperLeft;
        if(alignment==TextAnchor.MiddleRight || alignment==TextAnchor.LowerRight || alignment==TextAnchor.UpperRight)horzAlign=TextAnchor.UpperRight;
        UIVertex []tmpVert=new UIVertex[4];

        
        float lineHeight=(float)fontSize* (float)font.lineHeight/(float)font.fontSize ;
        float sizeFraction=(float)lineHeight/(GetComponent<RectTransform>().rect.size.y);
        CalculateSlices(sizeFraction);
        
        // the textgenerator allows us to layout text (and to find line lengths etc.)
        TextGenerator generator = new TextGenerator();
        TextGenerationSettings settings = new TextGenerationSettings();

        vh.Clear();

        Vector2 totalSize=GetComponent<RectTransform>().rect.size;


        settings.textAnchor = horzAlign;
        settings.color = color;
        settings.pivot = new Vector2(0.5f,0.5f);
        settings.richText = true;
        settings.font = font;
        settings.fontSize = fontSize;
        settings.fontStyle = FontStyle.Normal;
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        settings.horizontalOverflow=HorizontalWrapMode.Wrap;
        settings.lineSpacing = 1;
        settings.scaleFactor=1f;
        settings.generateOutOfBounds = false;
        
        List<string> sliceLines=new List<string>();
        List<Rect> slicePositions=new List<Rect>();

        // 
        
        if(alignment==TextAnchor.UpperCenter || alignment==TextAnchor.UpperRight || alignment==TextAnchor.UpperLeft)
        {
            // top down - easy case - just wrap normally and put each line into a slice in turn
            string textLeft=text;
            for(int c=0;c<slices.Count;c++)
            {
                float thisWidth=slices[c].width*totalSize.x;
                
                //print(slices[c].y);
                Rect r = slices[c];
                settings.generationExtents = new Vector2(thisWidth,totalSize.y);
                generator.Populate(textLeft, settings);
                float maxVertex=generator.verts.Count;
//                print(textLeft+","+generator.lines.Count+","+thisWidth+","+totalSize.x);
                slicePositions.Add(slices[c]);
                if(generator.lines.Count>1)
                {
                    sliceLines.Add(textLeft.Substring(0,generator.lines[1].startCharIdx));
//                    maxVertex=generator.lines[1].startCharIdx*4;
                    textLeft=textLeft.Substring(generator.lines[1].startCharIdx);
                }else
                {
                    sliceLines.Add(textLeft);
                    break;
                }
            }
        }else if(alignment==TextAnchor.LowerCenter || alignment==TextAnchor.LowerRight || alignment==TextAnchor.LowerLeft)
        {
            // bottom up - hard case - try 1 slice, then 2 slices until whole text is used or whole slices are used..
            
            for(int startSlice=slices.Count-1;startSlice>=0;startSlice--)
            {
                sliceLines.Clear();
                slicePositions.Clear();
                string textLeft=text;
                for(int c=startSlice;c<slices.Count;c++)
                {
                    Rect r = slices[c];
                    float thisWidth=r.width*totalSize.x;
                    settings.generationExtents = new Vector2(thisWidth,totalSize.y);
                    generator.Populate(textLeft, settings);
                    float maxVertex=generator.verts.Count;
    //                print(textLeft+","+generator.lines.Count+","+thisWidth+","+totalSize.x);
                    slicePositions.Add(r);
                    if(generator.lines.Count>1)
                    {
                        sliceLines.Add(textLeft.Substring(0,generator.lines[1].startCharIdx));
    //                    maxVertex=generator.lines[1].startCharIdx*4;
                        textLeft=textLeft.Substring(generator.lines[1].startCharIdx);
                    }else
                    {
                        sliceLines.Add(textLeft);
                        textLeft="";
                        break;
                    }
                }
                if(textLeft.Length==0)
                {
                    break;
                }
            }
        }else if(alignment==TextAnchor.MiddleCenter || alignment==TextAnchor.MiddleRight || alignment==TextAnchor.MiddleLeft)
        {
            // middle vertical alignment
            // try 1 line, 2 line and so on until we get one that fits the text
            // 
            for(int numSlices=1;numSlices<=slices.Count;numSlices++)
            {
                bool offset=(slices.Count&1)!=(numSlices&1);
                int startSlice=offset?(offsetSlices.Count/2)-numSlices/2:(slices.Count/2)-numSlices/2;
                int endSlice=numSlices+startSlice;
                print("try slices: "+numSlices+":"+offset+":"+startSlice);
                sliceLines.Clear();
                slicePositions.Clear();
                string textLeft=text;
                for(int c=startSlice;c<endSlice;c++)
                {
                    Rect r = offset?offsetSlices[c]:slices[c];
                    float thisWidth=r.width*totalSize.x;
                    settings.generationExtents = new Vector2(thisWidth,totalSize.y);
                    generator.Populate(textLeft, settings);
                    float maxVertex=generator.verts.Count;
                    slicePositions.Add(r);
                    if(generator.lines.Count>1)
                    {
                        sliceLines.Add(textLeft.Substring(0,generator.lines[1].startCharIdx));
                        textLeft=textLeft.Substring(generator.lines[1].startCharIdx);
                    }else
                    {
                        sliceLines.Add(textLeft);
                        textLeft="";
                        break;
                    }
                }
                if(textLeft.Length==0)
                {
                    // filled up this one just right
                    break;
                }
            }
        }
        for(int c=0;c<sliceLines.Count;c++)
        {
            Rect drawRect=slicePositions[c];
            float thisWidth=drawRect.width*totalSize.x;
            settings.textAnchor = horzAlign;
            settings.color = color;
            settings.generationExtents = new Vector2(thisWidth,totalSize.y);
            settings.pivot = new Vector2(0.5f,0.5f);
            settings.richText = true;
            settings.font = font;
            settings.fontSize = fontSize;
            settings.fontStyle = FontStyle.Normal;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.horizontalOverflow=HorizontalWrapMode.Wrap;
            settings.lineSpacing = 1;
            settings.scaleFactor=1f;
            settings.generateOutOfBounds = false;
            generator.Populate(sliceLines[c], settings);
            for(int i=0;i<generator.verts.Count;i+=4)
            {
                tmpVert[0]=generator.verts[i];
                tmpVert[1]=generator.verts[i+1];
                tmpVert[2]=generator.verts[i+2];
                tmpVert[3]=generator.verts[i+3];
                for(int k=0;k<4;k+=1)
                {
                    tmpVert[k].position.y-=drawRect.y*totalSize.y;
                    tmpVert[k].position.x+=thisWidth*.5f;
                    tmpVert[k].position.x-=totalSize.x*.5f;
                    tmpVert[k].position.x+=drawRect.x*totalSize.x;
                }
                vh.AddUIVertexQuad(tmpVert);
            }
        }
    }
}
