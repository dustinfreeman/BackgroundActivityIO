import tags
import sys

def showinfo(kntfilepath):
    print("Parsing: " + kntfilepath)

    f = open(kntfilepath, 'r')

    top_level_tag_count = {}
    
    try:
        while(True):
            tag_chunk = tags.read_tag_metadata(f)

            if not tag_chunk.tag in top_level_tag_count:
                top_level_tag_count[tag_chunk.tag] = 0
            top_level_tag_count[tag_chunk.tag] += 1
                
            print(tag_chunk)

            
    except Exception as e:
        #print "probably an end of file in parser"
        pass
    
    print("Printing Tag Chunk Count: ")
    for tag in top_level_tag_count:
        print("Tag: " + tags.get_tag_name(tag) +\
              " \tcount: " + str(top_level_tag_count[tag]))
    
    
    f.close()
    
    
if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("usage: bainfo [binary .knt file]\n\t shows info of background activity format data in file")
    else:
        print("Parsing tag chunks in file...")
        showinfo(sys.argv[1])

    
