#this file used for assessing time-continuous data quality
import sys
import tags

def find_ts_breaks(filepath, log_output):
    #parse a big file
    f = open(filepath,'rb')
    f_out = open(log_output,"w")
    #get file
    #tag_chunk = tags.read_tag_metadata(f)


    #iterate through file, for each chunk printing out
    VERBOSE = False
    last_s = ""
    last_timestamp = -1

    try:
        while (True):
            s = ''
            
            s += "\n" + "======"
            # File Location
            s += "\n" + "location: \t" + str(f.tell())

            tag_chunk = tags.read_to_timestamp(f)

            # Tag names
            s += "\n" + "tag: \t" + str(tag_chunk.tag)   
            s += "\n" + "tag length: \t" + str(tag_chunk.chunkLength)   
            # mTimestamps
            if tags.IsTimestampTagged(tag_chunk.tag):
                s += "\n" + "timestamp: \t" + str(tag_chunk.mTimestamp)

                #BREAK CHECK
                # ahead by more than one second.
                if last_timestamp < 0 or\
                    tag_chunk.mTimestamp > last_timestamp + 1*(10**7) or\
                    tag_chunk.mTimestamp < last_timestamp:
                    print "BREAK! ======================"
                    f_out.write("BREAK! ======================")
                    print last_s
                    f_out.write(last_s + "\n")
                    print s
                    f_out.write(s + "\n")

                last_timestamp = tag_chunk.mTimestamp

            if VERBOSE:
                print s

            last_s = s

    except Exception as e:
        print "likely end of file"
        print e

    f_out.close()

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("usage: ts_break_finder [input .knt file] [output .txt file]")
    else:
        inputknt = sys.argv[1]
        outputtxt = sys.argv[2]
        find_ts_breaks(inputknt, outputtxt)
    
