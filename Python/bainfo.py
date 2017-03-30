import tags
import sys

def showinfo(kntfilepath, verbose = False):
    print("Parsing: " + kntfilepath)

    f = open(kntfilepath, 'r')

    top_level_tag_count = {}
    
    try:
        while(True):
            tag_chunk = tags.read_to_timestamp(f)

            if not tag_chunk.tag in top_level_tag_count:
                top_level_tag_count[tag_chunk.tag] = 0
            top_level_tag_count[tag_chunk.tag] += 1
                
            if verbose:
                try:
                    print(str(tag_chunk) + "\ttimestamp: " + str(tag_chunk.mTimestamp))
                except AttributeError:
                    print(tag_chunk)

            
    except Exception as e:
        #print "probably an end of file in parser"
        #print("Parsing Exception " + str(e))
        pass
    
    print("Printing Tag Chunk Count: ")
    for tag in top_level_tag_count:
        print("Tag: " + tags.get_tag_name(tag) +\
              " \tcount: " + str(top_level_tag_count[tag]))
    
    
    f.close()
    
    
if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("usage: \tbainfo <.knt file> [--verbose] \n\t shows info of background activity format data in file")
    else:
        print("Parsing tag chunks in file...")
        verbose = False
        if "--verbose" in sys.argv:
            verbose = True
        showinfo(sys.argv[1], verbose)

    
