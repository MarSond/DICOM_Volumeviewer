import pydicom 
import os
import numpy as np
import argparse
import math

separator="|"
block_separator="#"

parser = argparse.ArgumentParser(description='DICOM Ordner in Zwischendatei übersetzen.')
parser.add_argument("-s", "--source", required=True, help="Ordner mit Dateien der DICOM Serie")
parser.add_argument("-t", "--target", required=True, help="Zieldatei <Vollständiger Pfad>")
args=vars(parser.parse_args())

# Print iterations progress
def printProgressBar (iteration, total, prefix = '', suffix = '', decimals = 1, length = 100, fill = '#', printEnd = "\r"):
	percent = ("{0:." + str(decimals) + "f}").format(100 * (iteration / float(total)))
	filledLength = int(length * iteration // total)
	bar = fill * filledLength + '-' * (length - filledLength)
	print('\r%s |%s| %s%% %s' % (prefix, bar, percent, suffix), end = printEnd)
	# Print New Line on Complete
	if iteration == total: 
		print()

base_path= args["source"]
targetFile= args["target"]
if os.path.exists(targetFile):
	os.remove(targetFile)

fileList = []
loadedList= []
# r=root, d=directories, f = files
for r, d, f in os.walk(base_path):
	for file in f:
		fileList.append(os.path.join(r, file))

fCount=len(fileList)
for f in fileList:
	loadedList.append(pydicom.dcmread(f))

print(str(fCount)+' Dateien gefunden')
firstIm=loadedList[0]
dataArr=firstIm.pixel_array
numRows=dataArr.shape[0]
numColls=dataArr.shape[1]
print('Das erste Bild hat {} x {} Pixel'.format(numRows,numColls))
# PRINT HEADER   ImageCount | rows | Colls | Dicke aus Metadaten
file=open(targetFile,"x",newline='')
file.write(block_separator+str(fCount)+separator+str(numRows)+separator+str(numColls)+separator+str(math.trunc(firstIm[0x0018,0x0050].value))+"\n")
file.close()

# START FILES APPENDING
def append_datafile(dcmImage, number):
	file=open(targetFile,"at",newline='')
	file.write(block_separator+str(number)+"\n")
	data=dcmImage.pixel_array
	rescaleIntercept = dcmImage[0x0028,0x1052].value
	rescaleSlope = dcmImage[0x0028,0x1053].value
	
	data=data * rescaleSlope + rescaleIntercept
	for r in range(numRows):
		line=""
		for col in range(numColls):
			line+=str(int(data[r][col]))+separator
		writeLine=line[:-1]+"\n"
		file.write(writeLine)
	file.close()

counter=0
input("Schreiben in Datei starten? ")
for dcmFile in loadedList:
	printProgressBar(counter, len(loadedList), prefix = 'Fortschritt ', suffix = 'Fertig', decimals = 2)
	counter+=1
	append_datafile(dcmFile,counter)
printProgressBar(counter, len(loadedList), prefix = 'Fortschritt ', suffix = 'Fertig', decimals = 2)
print("All files written to "+targetFile)