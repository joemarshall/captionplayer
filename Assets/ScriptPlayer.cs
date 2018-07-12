using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ScriptPlayer : MonoBehaviour {

    public Canvas canvas;
    
    OSC osc;

    class AreaChange
    {
        public string text="";
        public string displayArea;
        public Texture2D image;
        public string movieName;
        
    }
    
    class ScriptItem
    {
        public string cueID;
        public List<AreaChange> changes=new List<AreaChange>();
    };
    
    class DisplayArea
    {
        public string fontName;
        public int fontSize;
        
        public string name;
        public float x0,y0,x1,y1;
        public int r,g,b;
        
        public DisplayArea()
        {
            x0=0.1f;y0=0.1f;x1=.9f;y1=.9f;
            r=255;g=255;b=255;
            fontSize=36;
            fontName="Arial";
            playedVid=false;
            preparedVid=false;
        }
        
        public Text text;
        public RawImage image;
        public VideoPlayer video;
        public RenderTexture vidTex;
        public RawImage videoImage;
        
        public bool playedVid;
        public bool preparedVid;
        
    };
        
    string GetScriptItems() 
    {        
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "threeway.txt");
        string result;
        if (filePath.Contains("://")) {
            WWW www = new WWW(filePath);
            result = www.text;
        } else
            result = System.IO.File.ReadAllText(filePath);
        return result;
    }    

    Texture2D LoadScriptImage(string path) 
    {        
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath,  path);
        Texture2D result;
        if (filePath.Contains("://")) {
            WWW www = new WWW(filePath);
            result = www.texture;
        } else
        {
            Texture2D tex=new Texture2D(2,2);
            byte[] data=System.IO.File.ReadAllBytes(filePath);
            ImageConversion.LoadImage(tex, data);
            result = tex;
        }
        return result;
    }    

    string LoadScriptVideo(string path) 
    {        
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath,  path);
        return filePath;
        // if (filePath.Contains("://")) {
            // return filePath;
        // } else
        // {
            // return "file://"+filePath;
        // }
     }    
    
    
    
    List<ScriptItem> items=new List<ScriptItem>();
    List<DisplayArea> areas=new List<DisplayArea>();
    
    int curPosition=-1;
    int newPosition=0;
    
    bool inFade=false;
    float fadeTime=0f;
    float fadeTotal=0.25f;

    DisplayArea GetDisplayArea(string areaName)
    {
        for(int c=0;c<areas.Count;c++)
        {
            if(areas[c].name==areaName)
            {
                return areas[c];
            }
            
        }
        return areas[0];
    }
    
    
	// Use this for initialization
	void Start () 
    {
        osc=GetComponent<OSC>();
        osc.SetAllMessageHandler(OnOSCMessage);
		string allScript=GetScriptItems();
        string[] lines=allScript.Split('\n');
        ScriptItem curItem=null;
        AreaChange newAreaChange=null;
        foreach(string lineRaw in lines)
        {
            string line=lineRaw.Trim();
            if(line.StartsWith("AREA:"))
            {
                DisplayArea newArea=new DisplayArea();
                string []values=line.Substring(5).Split(',');
                for(int c=0;c<values.Length;c++)
                {
                    switch(c)
                    {
                        case 0:
                            newArea.name=values[c];
                            break;
                        case 1:
                            newArea.fontName=values[c];
                            break;
                        case 2:
                            int.TryParse(values[c],out newArea.fontSize);
                            break;
                        case 3:
                            int.TryParse(values[c],out newArea.r);
                            break;
                        case 4:
                            int.TryParse(values[c],out newArea.g);
                            break;
                        case 5:
                            int.TryParse(values[c],out newArea.b);
                            break;
                        case 6:
                            float.TryParse(values[c],out newArea.x0);
                            break;
                        case 7:
                            float.TryParse(values[c],out newArea.y0);
                            break;
                        case 8:
                            float.TryParse(values[c],out newArea.x1);
                            break;
                        case 9:
                            float.TryParse(values[c],out newArea.y1);
                            break;
                    }
                }
                
             
                GameObject imgChild=new GameObject("img "+newArea.name);
                imgChild.transform.SetParent(canvas.transform,false);
                RawImage image=imgChild.AddComponent<RawImage>();
                RectTransform tf2=imgChild.GetComponent<RectTransform>();
                tf2.anchorMin=new Vector2(newArea.x0,newArea.y0);
                tf2.anchorMax=new Vector2(newArea.x1,newArea.y1);
                tf2.offsetMin=new Vector2(0,0);
                tf2.offsetMax=new Vector2(0,0);
                newArea.image=image;
                newArea.image.enabled=false;


                RenderTexture vidTex=new RenderTexture(1024,1024,0);
                vidTex.Create();

                GameObject vidChild=new GameObject("vid "+newArea.name);
                vidChild.transform.SetParent(canvas.transform,false);

                RawImage vImage=vidChild.AddComponent<RawImage>();
                vImage.texture=vidTex;
                RectTransform tf3=vidChild.GetComponent<RectTransform>();
                tf3.anchorMin=new Vector2(newArea.x0,newArea.y0);
                tf3.anchorMax=new Vector2(newArea.x1,newArea.y1);
                tf3.offsetMin=new Vector2(0,0);
                tf3.offsetMax=new Vector2(0,0);
                
                VideoPlayer vidPlayer=vidChild.AddComponent<VideoPlayer>();
                vidPlayer.renderMode=VideoRenderMode.RenderTexture;
                vidPlayer.targetTexture=vidTex;
                newArea.video=vidPlayer;
                newArea.vidTex=vidTex;
                newArea.videoImage=vImage;


                
                GameObject textChild=new GameObject("txt "+newArea.name);
                textChild.transform.SetParent(canvas.transform,false);
                Text txt = textChild.AddComponent<Text>();
                txt.text = "";
                txt.color=new Color(((float)newArea.r)/255.0f,((float)newArea.g)/255.0f,((float)newArea.b)/255.0f);
                txt.alignment=TextAnchor.MiddleLeft;
                txt.font=Font.CreateDynamicFontFromOSFont(newArea.fontName,36);
                txt.fontSize=newArea.fontSize;

                RectTransform tf=textChild.GetComponent<RectTransform>();
                tf.anchorMin=new Vector2(newArea.x0,newArea.y0);
                tf.anchorMax=new Vector2(newArea.x1,newArea.y1);
                tf.offsetMin=new Vector2(0,0);
                tf.offsetMax=new Vector2(0,0);
                
                newArea.text=txt;                
                
                areas.Add(newArea);
            }else
            {
                print(line);
                bool firstLine=(line.Length>0 && line[0]=='#');
                if(firstLine)
                {
//                    print(line);
                    if(newAreaChange!=null)
                    {
                        newAreaChange.text=newAreaChange.text.Trim();
                        curItem.changes.Add(newAreaChange);
                    }
                    newAreaChange=new AreaChange();
                    string []cueData=line.Substring(1).Split(',');
                    if(cueData.Length>1)
                    {
                        newAreaChange.displayArea=cueData[1];
                    }
                    curItem=null;
                    for(int c=0;c<items.Count;c++)
                    {
                        if(items[c].cueID==cueData[0])
                        {
                            curItem=items[c];
                            break;
                        }
                    }
                    if(curItem==null)
                    {
                        curItem=new ScriptItem();
                        curItem.cueID=cueData[0];
                        items.Add(curItem);
                        print(items.Count+"!");
                    }                    
                }else
                {
                    if(newAreaChange!=null)
                    {
                        if(line.StartsWith("[") && line.IndexOf("]")>0)
                        {
                            newAreaChange.image=LoadScriptImage(line.Substring(1,line.IndexOf("]")-1));
                        }else  if(line.StartsWith("{") && line.IndexOf("}")>0)
                        {
                            newAreaChange.movieName=LoadScriptVideo(line.Substring(1,line.IndexOf("}")-1));
                        }else
                        {
                            newAreaChange.text+=line+"\n";
                        }
                    }
                }
            }
        }
        if(newAreaChange!=null)
        {
            newAreaChange.text=newAreaChange.text.Trim();
            curItem.changes.Add(newAreaChange);
        }        
	}
	
	// Update is called once per frame
	void Update () 
    {
		if(Input.GetKeyDown("space"))
        {
            newPosition+=1;
            if(newPosition>=items.Count)
            {
                newPosition=0;
            }
            print(newPosition+"!");
                
        }
        if(curPosition!=newPosition)
        {
            ChangeDisplay(newPosition);
        }
	}
    
    void PreloadNextVideos(int position)
    {
        for(int a=0;a<areas.Count;a++)
        {
            DisplayArea thisArea=areas[a];
            //print(thisArea.video.url);
            for(int c=position;c<items.Count;c++)
            {
                //print(items[c].displayArea);
                for(int ch=0;ch<items[c].changes.Count;ch++)
                {
                    AreaChange change=items[c].changes[ch];
                    if(change.displayArea==thisArea.name || (a==0 && change.displayArea==null) )
                    {
                        if(change.movieName!=null)
                        {
                            if(thisArea.preparedVid==true && change.movieName==thisArea.video.url)break;
                            print("Prepare video:"+change.movieName);
                            thisArea.video.playOnAwake=false;
                            thisArea.video.url=change.movieName;
                            thisArea.video.Prepare();
                            thisArea.preparedVid=true;
                            break;
                        }
                    }
                }
            }
        }
    }
    
    void AlterOffsetForRatio(RectTransform outputRect, float inputWidth,float inputHeight)    
    {
        // get ratio of the outputRect
        outputRect.offsetMin=new Vector2(0,0);
        outputRect.offsetMax=new Vector2(0,0);
        
        Rect baseRect=outputRect.rect;
        
        float baseAspect=(float)baseRect.width/(float)baseRect.height;
        
        float inputAspect=inputWidth/inputHeight;
        
        if(baseAspect>inputAspect)
        {
//            print("Woo"+inputWidth+":"+inputHeight+":"+outputRect.rect);
            // gaps at left and right
            float targetWidth=((float)baseRect.height*inputAspect);            
            float gapAmount=0.5f*(baseRect.width-targetWidth);
            print(baseAspect+","+inputAspect+":"+targetWidth);
            outputRect.offsetMin=new Vector2(gapAmount,0);
            outputRect.offsetMax=new Vector2(-gapAmount,0);
        }else
        {
//            print("Yay"+inputWidth+":"+inputHeight+":"+outputRect.rect);
            // gaps at top and bottom
            float targetHeight=((float)baseRect.width/inputAspect);            
            float gapAmount=0.5f*(baseRect.height-targetHeight);
            outputRect.offsetMin=new Vector2(0,gapAmount);
            outputRect.offsetMax=new Vector2(0,-gapAmount);
        }
        
       
    }
    
    void ChangeDisplay(int position)
    {
        if(!inFade)
        {
            inFade=true;
            fadeTime=0;
        }
        // fade out any areas in the current position, ready to blink in the new one
        fadeTime+=Time.deltaTime;
        if(fadeTime<fadeTotal)
        {
            float fadeAlpha=(fadeTotal-fadeTime)/fadeTotal;
            for(int ch=0;ch<items[position].changes.Count;ch++)
            {
                AreaChange change=items[position].changes[ch];
                DisplayArea area=GetDisplayArea(change.displayArea);
                area.videoImage.color=new Color(1,1,1,fadeAlpha);
                area.image.color=new Color(1,1,1,fadeAlpha);
                area.text.color=new Color(area.text.color.r,area.text.color.g,area.text.color.b,fadeAlpha);
            }
            return;
        }
        
        for(int ch=0;ch<items[position].changes.Count;ch++)
        {
            AreaChange change=items[position].changes[ch];
            DisplayArea area=GetDisplayArea(change.displayArea);
            if(change.movieName!=null)
            {
                if(area.video.url!=change.movieName || !area.preparedVid)
                {
                    area.video.url=change.movieName;
                    area.video.Prepare();                
                    area.video.playOnAwake=false;
                    area.preparedVid=true;
                }
                if(!area.video.isPrepared)
                {
                    print("Not prepared");
                    return;
                }
                print("Play video:"+change.movieName+":"+area.video.texture.width+","+area.video.texture.height);
                // change image limits to fit video in the frame right
                float vw=area.video.texture.width;
                float vh=area.video.texture.height;
                
                Rect newUVRect;
                // first make texture uv rect exactly video size
                if(vw<vh)
                {
                    // sides of texture need to go
                    float scaling=vw/vh;                
                    newUVRect=new Rect((1f-scaling)/2f,0,scaling,1);
                    area.videoImage.uvRect=newUVRect;                
                }else
                {
                    // top of texture needs to go
                    // size it so that pixels are square
                    float scaling=vh/vw;                
                    newUVRect=new Rect(0,(1f-scaling)/2f,1,scaling);
                    area.videoImage.uvRect=newUVRect;
                }
                AlterOffsetForRatio(area.videoImage.GetComponent<RectTransform>(),vw,vh);            

                RenderTexture rt = UnityEngine.RenderTexture.active;
                UnityEngine.RenderTexture.active = area.vidTex;
                GL.Clear(true, true, Color.clear);
                UnityEngine.RenderTexture.active = rt;
                            
                area.video.Play();
                area.playedVid=true;
                area.videoImage.enabled=true;
                area.videoImage.texture=area.vidTex;
                area.image.enabled=false;
            }else
            {
                // stop any video in this area
                if(area.playedVid )
                {
                    area.video.Stop();
                    area.playedVid=false;
                    area.preparedVid=false;
                }
                area.videoImage.enabled=false;
                if(change.image!=null)
                {
                    // an image, load it
                    area.image.enabled=true;
                    area.image.texture=change.image;
                    AlterOffsetForRatio(area.image.GetComponent<RectTransform>(),change.image.width,change.image.height);
                }else
                {
                    area.image.enabled=false;
                }
            }
            area.text.text=change.text;
            // make sure any future videos are loaded
        }
        PreloadNextVideos(position);
        curPosition=position;
        if(inFade)
        {
            inFade=false;
            for(int ch=0;ch<items[position].changes.Count;ch++)
            {
                AreaChange change=items[position].changes[ch];
                DisplayArea area=GetDisplayArea(change.displayArea);
                area.videoImage.color=new Color(1,1,1,1);
                area.image.color=new Color(1,1,1,1);
                area.text.color=new Color(area.text.color.r,area.text.color.g,area.text.color.b,1);
            }
            
        }
    }
    
      public void OnOSCMessage( OscMessage oscM )
      {
          print(oscM.address);
          string targetName=oscM.address.Substring(1).Replace("/"," ");
          for(int c=0;c<items.Count;c++)
          {
              if(string.Equals(items[c].cueID,targetName,StringComparison.CurrentCultureIgnoreCase))
              {
                  newPosition=c;
                  break;
              }
          }
      }

}
