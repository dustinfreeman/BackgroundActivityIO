import tags
import sys

def showinfo(kntfilepath):
    print("Parsing: " + kntfilepath)

    f = open(kntfilepath, 'r')

    for i in range(10):
        print(tags.read_tag_metadata(f))


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("usage: bainfo [binary .knt file]\n\t shows info of background activity format data in file")
    else:
        print("Parsing tag chunks in file...")
        showinfo(sys.argv[1])

    
