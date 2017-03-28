import struct

TAG_SIZE = 4

def read_tag_char(file):
    tag_byte = file.read(TAG_SIZE)
    try:
        return tag_byte.decode("utf-8")
    except Exception as e:
        print e
        print "Could not decode: " + str(tag_byte)

def read_int(file):
    return int(struct.unpack('i', file.read(4))[0])

def read_long(file):
    return long(struct.unpack('q', file.read(8))[0])


TAG_DEFINITION_FILE = "../tag_definitions.txt"

tag_definitions = []

class TagDef:
    def __init__(self,tag_name,tag_char):
        self.tag_name = tag_name
        self.tag_char = tag_char

def get_tag_name(tag_char):
    for tag_pair in tag_definitions:
        if tag_pair.tag_char == tag_char:
            return tag_pair.tag_name
    return None

def get_tag_char(tag_name):
    for tag_pair in tag_definitions:
        if tag_pair.tag_name == tag_name:
            return tag_pair.tag_char
    return None

def IsTimestampTagged(tag_char):
    tag_name = get_tag_name(tag_char)
    if "Frame" in tag_name:
        return True
    return False

class TagChunk:
    def __init__(self):
        self.tag = None
        self.chunkLength = 0

    def __str__(self):
        return "Tag Chunk: " + self.tag + "; byte length: " + str(self.chunkLength)
    
        
class Frame(TagChunk): 
    #the time-tagged version of TagChunk
    def __init__(self):
        self.mTimestamp = None
        self.kTimestamp = None

#printing is only relevant if timestamps have been read    
#    def __str__(self):
#        return "Frame Chunk. Tag Chunk: " + self.tag + "; byte length: " + str(self.chunkLength) +\
#            "; machine ts: " + str(self.mTimestamp) + "; kinect ts " + str(self.kTimestamp)
        

def read_tag_metadata(file, skip_to_chunk_end = True):
    #need to decode from byte.
    tag_char = read_tag_char(file)

    if IsTimestampTagged(tag_char):
        tag_chunk = Frame()
    else:
        tag_chunk = TagChunk()

    tag_chunk.tag = tag_char
    tag_chunk.chunkLength = read_int(file)
    # print tag_chunk.chunkLength
    # print tag_chunk.chunkLength is int

    if skip_to_chunk_end:
        file.seek(tag_chunk.chunkLength,1) #seek relative to current position
    else:
        pass #file pointer starts at beginning of RIFF data

    return tag_chunk

def read_to_timestamp(file, skip_to_chunk_end = True):
    #reads a tag chunk to mTimestamp
    tag_chunk = read_tag_metadata(file, False)

    read_past = 0
    if isinstance(tag_chunk, Frame):
        for i in range(2):
            #read both timestamps

            tag_char = read_tag_char(file)
            chunkLength = read_int(file)
            t_value = read_long(file)
            # print get_tag_name(tag_char)
            # print t_value
            if get_tag_name(tag_char) == "mTimestamp":
                tag_chunk.mTimestamp = t_value
            if get_tag_name(tag_char) == "kTimestamp":
                tag_chunk.kTimestamp = t_value

            #4 = int size
            read_past += TAG_SIZE + 4 + chunkLength

    if skip_to_chunk_end:
        file.seek(tag_chunk.chunkLength - read_past,1) #seek relative to current position
        #print "seek: " + str(tag_chunk.chunkLength - read_past)
    else:
        pass     

    return tag_chunk

def init_tags():
    #parse tag definitions
    tag_file = open(TAG_DEFINITION_FILE,'r')
    for line in tag_file:
        line = line.strip()
        if line == '':
            continue

        line_split = line.split('=')

        tag_name = line_split[0].strip()
        tag_char = line_split[1].strip()

        tag_definitions.append(TagDef(tag_name,tag_char))

init_tags()
