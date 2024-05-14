###############################################################################
###############################################################################
#                   Normalization and PCA
###############################################################################
###############################################################################
#### Workflow 
# Get annot colors
# Prcomp
# Pairs plot

#### Input objects
# Endo Data Matrix
#     Log transformed, centered and scaled data ((x - mean) / sd)
#     Columns = genes
#     Rows    = Sample Filenames

sink(file = "C:\\ProgramData\\UQCmodule\\tmp\\PCA\\R_log.txt", append = TRUE, type = "output", split = TRUE)

redWhiteBlue = colorRampPalette(c("dodgerblue", "snow2", "red"), space = "Lab")
colorVector = c("red", "green", "blue", "gold", "black", "orange", "magenta", "purple", "snow", "tomato")

# Convert variable range to 1:100 and map to 100 color palette
getContSideCols = function(values)
{
  # Transform old range of values to 1:100
  oldRange = max(values) - min(values);
  ints = vector("numeric", length = length(values));
  for(i in 1:length(values))
  {
    ints[i] = (((values[i] - min(values)) * 99) / oldRange) + 1
  }
  cols = redWhiteBlue(100);
  out = cols[ints];
  return(out);
}

# Map variable levels to colors in a 10:color vector or ...
# ... if number of levels greater than 10, map to a color ramp made from the color vector
getCatSideCols = function(values)
{
  fact = as.factor(values);
  n = nlevels(fact);
  cols = c();
  if(n <= 10)
  {
    cols = colorVector[c(1:n)];
  } else {
    cols = colorVector;
  }
  pal = colorRampPalette(colors = cols, space = "Lab");
  varColors = pal(n);
  out = list(cols = varColors[fact], fact = fact, varCols = varColors)
  return(out)
}

RunPCA = function()
{
  cat("Reading the data");
  tmp.pca.path = "C:\\ProgramData\\UQCmodule\\tmp\\PCA";
  # arguments
  arg.path = file.path(tmp.pca.path, "args.csv", fsep = "\\");
  argFile = read.csv(arg.path, row.names = 1, check.names = FALSE, stringsAsFactors = FALSE);
  annot.name = argFile[1,1];
  isCatagorical = argFile[2,1] == "0";
  date.string = argFile[3,1];
  
  # expression data
  expr.data.path = file.path(tmp.pca.path, "expr.csv", fsep = "\\");
  file.in = read.csv(expr.data.path, row.names = 1, check.names = FALSE, stringsAsFactors = FALSE);
  dat = as.matrix(file.in);
  # annot data
  annot.path = file.path(tmp.pca.path, "annot.csv", fsep = "\\");
  file2.in = read.csv(annot.path, row.names = 1, check.names = FALSE, stringsAsFactors = FALSE);
  
  cat("Generate annot colors");
  annotCols = list();
  cols = vector("character", length = nrow(dat))
  if(isCatagorical)
  {
    annotCols = getCatSideCols(file2.in[,1]);
    cols = annotCols$cols
  } else {
    annotCols = getContSideCols(file2.in[,1]);
    cols = annotCols
  }
  
  cat("Running PC");
  pc = prcomp(x = dat, retx = TRUE);
  x = pc$x[, 1:min(4, dim(pc$x)[2])]; #take up to 1st 4 PCs
  
  cat("generating pairs figure");
  result.path = paste(tmp.pca.path, "\\", date.string, "_plot.png", sep="")
  png(file = result.path, width = 950, height = 800);
  percentages = round((pc$sdev^2)[1:4]/sum(pc$sdev^2),2);
  pairs(x, col=cols, xaxt="n", yaxt="n", cex = 2, pch = 16, 
        labels = paste("PC", 1:4, " - ", percentages, sep=""), oma = c(1,1,8,20),
        main = paste("PCA: points colored by", annot.name, sep = " "));
  if(isCatagorical)
  {
    par(xpd = TRUE);
    legend("topright", 
           legend = levels(annotCols$fact), 
           col = annotCols$varCols[1:length(levels(annotCols$fact))], 
           pch = 16, 
           ncol = 1,
           title = annot.name);
  } else{
    min = signif(min(file2.in[,1]),3);
    mid = signif(median(file2.in[,1]),3);
    max = signif(max(file2.in[,1]),3);
    legCols = redWhiteBlue(100);
    par(xpd = TRUE);
    legend("topright", 
           legend = c(paste("min-",min, sep=""), 
                      paste("mid-",mid, sep=""), 
                      paste("max-",max, sep="")), 
           col = c(legCols[1], legCols[50], legCols[100]),
           pch = 16,
           ncol = 1,
           title = annot.name);
  }
  
  dev.off();
  
  # Create and save gene loadings table in tmp
  ldings0 = pc$rotation[,c(1:4)]
  ldings = ldings0[order(ldings0[,1], decreasing = TRUE),]
  colnames(ldings) <- paste("PC", 1:4, " (", percentages, ")", sep="")
  lding.path = paste(tmp.pca.path, "\\",date.string, "_loadings.csv", sep="")
  write.csv(ldings, file = lding.path)
}

RunPCA();

sink()