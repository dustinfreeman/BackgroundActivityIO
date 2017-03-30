import tags

import sys
import datetime

def timestamp2DateTime(mTimestamp):
    #TODO must correctly account for EST timezone
    dt = datetime.datetime.fromtimestamp(mTimestamp/10**8)
    return dt.strftime('%Y-%m-%d %H:%M:%S')
    
def showinfo(kntfilepath, verbose = False):
    print("Parsing: " + kntfilepath)

    f = open(kntfilepath, 'r')

    top_level_tag_count = {}

    first_ts = sys.maxint
    last_ts = -sys.maxint - 1
    
    try:
        while(True):
            tag_chunk = tags.read_to_timestamp(f)

            if not tag_chunk.tag in top_level_tag_count:
                top_level_tag_count[tag_chunk.tag] = 0
            top_level_tag_count[tag_chunk.tag] += 1

            try:
                if tag_chunk.mTimestamp < first_ts:
                    first_ts = tag_chunk.mTimestamp
                if tag_chunk.mTimestamp > last_ts:
                    last_ts = tag_chunk.mTimestamp
            except AttributeError:
                pass
            
            if verbose:
                try:
                    print(str(tag_chunk) + "\ttimestamp: " + str(timestamp2DateTime(tag_chunk.mTimestamp)))
                except AttributeError:
                    print(tag_chunk)

            
    except Exception as e:
        #print "probably an end of file in parser"
        if verbose:
            print("Parsing Exception: " + str(e))
        pass
    
    print("Printing Tag Chunk Overview: ")
    for tag in top_level_tag_count:
        print("Tag: " + tags.get_tag_name(tag) +\
              " \tcount: " + str(top_level_tag_count[tag]))
    print("First Timestamp:\t " + timestamp2DateTime(first_ts))
    print(" Last Timestamp:\t " + timestamp2DateTime(last_ts))
        
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

    
