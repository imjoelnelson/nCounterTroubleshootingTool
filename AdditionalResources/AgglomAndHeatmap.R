####################################################################################################################
##############       Script for clustering and heatmap from nSolver Troubleshooting Tool      ######################
####################################################################################################################
sink(file = "C:\\ProgramData\\UQCmodule\\tmp\\R_log.txt", type = "output", split = TRUE)

# Load required packages
packDir = "C:\\ProgramData\\q_TSapps_Rlibs"
packDir2 = "C:\\ProgramData\\nSolverAA_Rlibs\\2.0"
if(!dir.exists(packDir))
{
  dir.create(packDir)
}

if(dir.exists(packDir2))
{
  if(!require(gplots, lib.loc = packDir2))
  {
    packList = c("bitops_1.0-6.zip",
                 "gtools_3.5.0.zip", 
                 "gdata_2.18.0.zip", 
                 "caTools_1.17.1.zip",
                 "gplots_3.0.1.zip");
    packNames = c("bitops",
                  "gtools",
                  "gdata",
                  "caTools",
                  "gplots");
    resourcePath = "C:\\ProgramData\\UQCmodule\\Resources"
    for(i in 1:length(packNames))
    {
      if(!require(packNames[i], lib.loc = packDir, character.only = T))
      {
        install.packages(file.path(resourcePath, packList[i], fsep = "\\"), lib = packDir, repos = NULL, type = "win.binary")
      }
    }
    require(gplots, lib.loc = packDir)
  }
} else {
  packList = c("bitops_1.0-6.zip",
               "gtools_3.5.0.zip", 
               "gdata_2.18.0.zip", 
               "caTools_1.17.1.zip",
               "gplots_3.0.1.zip");
  packNames = c("bitops",
                "gtools",
                "gdata",
                "caTools",
                "gplots");
  resourcePath = "C:\\ProgramData\\UQCmodule\\Resources"
  for(i in 1:length(packNames))
  {
    if(!require(packNames[i], lib.loc = packdir, character.only = T))
    {
      install.packages(file.path(resourcePath, packList[i], fsep = "\\"), lib = packDir, repos = NULL, type = "win.binary")
    }
  }
  require(gplots, lib.loc = packDir)
}

#read arguments from argument file
argfile = read.table(file = "C:\\ProgramData\\UQCmodule\\tmp\\argfile.txt", sep = "\t", row.names = 1, stringsAsFactors = FALSE)
matrixPath = argfile[1,1] # Path to heatmap matrix values
hasCovariates = as.logical(argfile[2,1]) # Logical; are there sidecolor values
isSymCor = as.logical(argfile[3,1]) # bool for whether the heatmap is symmetrical correlation plot
resultPath = argfile[4,1] # Path to the resulting image
distanceMetric = gsub("@", "\"", argfile[5,1]) # Distance metric
sampDistMatPath = argfile[6,1] # Sample distance matrix path
isCategorical = as.logical(argfile[7,1]) #Logical indicating if sidecol variable is categorical. If FALSE, it is continuous; if TRUE it is categorical
covariateName = argfile[8,1] # character vector; name of the selected covariate; may be "" if none selected
geneDistMatPath = argfile[9,1] # Gene distance matrix path
isNormalized = argfile[10,1] # Addition to gene expression heatmap title to indicate if counts were raw or normalized

# create color palettes
blueBlackYellow = colorRampPalette(c("dodgerblue","black","yellow2"),space = "Lab")
yellowBlackBlue = colorRampPalette(c("yellow2","black","dodgerblue"),space = "Lab")
redWhiteBlue = colorRampPalette(c("dodgerblue", "white", "red"), space = "Lab")
colorVector = c("red", "green", "blue", "gold", "black", "orange", "magenta", "purple", "snow", "tomato")

# Convert variable range to 1:100 and map to 100 length color ramp
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

# Map variable levels to colors in color vector if levels <= 10, else map to color ramp based on color vector
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

#heatmap function
runHeatmap = function(matrix.path, has.covariates, covariate.name, samp.dist.mat.path, gene.dist.mat.path, is.symcor, result.path, distance.metric, is.categorical, hmCol, hmColAlt)
{
  #matrix.path=matrixPath;file2=hasCovariates;samp.dist.mat.path=sampDistMatPath;is.symcor=isSymCor;result.path=resultPath;distance.metric=distanceMetric;
  datafile0 = read.csv(matrix.path, row.names = 1, check.names = FALSE, stringsAsFactors = FALSE);
  datafile1 = datafile0[,-1]
  Covariate = c()
  covariateColors = list()
  if(is.categorical)
  {
    covariateColors = getCatSideCols(datafile0[,1])
    Covariate = covariateColors$cols
  } else {
    annots = as.numeric(datafile0[,1])
    Covariate = getContSideCols(annots)
  }
  
  showRowNames = FALSE;
  if(nrow(datafile1) < 100)
  {
    showRowNames = row.names(datafile1);
  } else {
    showRowNames = FALSE;
  }
  
  if(has.covariates)
  {
    cat("Extra variable\nsymmetric corr matrix\n")
    if(is.symcor)
    {
      sampDistMat = read.csv(samp.dist.mat.path, header = TRUE, row.names = 1);
      png(result.path, width = 1200, height = 900);
      heatmap.2(x = as.matrix(datafile1),
                Rowv = TRUE,
                Colv = "Rowv",
                distfun = function(x) {f=x;return(as.dist(sampDistMat))},
                hclustfun = function(x) {hclust(x, method = "complete")},
                dendrogram = "column",
                col = hmColAlt,
                symm = TRUE,
                trace = "none",
                ColSideColors = Covariate,
                margins = c(10,30),
                labCol = FALSE,
                labRow = showRowNames,
                keysize = 0.6,
                key.xlab = "Pearson Dist",
                density.info = "none",
                main = "Sample Pearson Correlation Heatmap");
      if(!is.categorical)
      {
        cols = redWhiteBlue(100);
        legend("topright",
               legend = c("Min", "Mid", "Max"),
               fill = c(cols[1], cols[50], cols[100]),
               title = covariate.name);
      } else {
        levs = levels(covariateColors$fact)
        legend("topright",
                legend = levs,
                fill = colorVector[c(1:length(levs))],
                title = covariate.name);
      }
      
    } else{
      cat("Extra variable\ngene expression matrix\n")
      sampDistMat = read.csv(samp.dist.mat.path, header = TRUE, row.names = 1);
      geneDistMat = read.csv(gene.dist.mat.path, header = TRUE, row.names = 1);
      sampDend = as.dendrogram(hclust(as.dist(sampDistMat), method = "complete"), hang = -1, check = TRUE);
      geneDend = as.dendrogram(hclust(as.dist(geneDistMat), method = "complete"), hang = -1, check = TRUE);
      png(result.path, width = 1200, height = 900);
      heatmap.2(x = as.matrix(datafile1),
                Rowv = sampDend,
                Colv = geneDend,
                col = hmCol,
                symm = FALSE,
                trace = "none",
                RowSideColors = Covariate,
                margins = c(10,30),
                labCol = FALSE,
                labRow = showRowNames,
                keysize = 0.6,
                key.xlab = distance.metric,
                density.info = "none",
                main = paste("Gene Expression and clustering (", distance.metric, "; ",isNormalized, ")", sep = ""));
      if(!is.categorical)
      {
        cols = redWhiteBlue(100);
        legend("topright",
               legend = c("Min", "Mid", "Max"),
               fill = c(cols[1], cols[50], cols[100]),
               title = covariate.name);
      } else {
        levs = levels(covariateColors$fact)
        legend("topright",
               legend = levs,
               fill = colorVector[c(1:length(levs))],
               title = covariate.name);
      }
    }
  } else{
    if(is.symcor)
    {
      cat("No extra variable\nsymmetric corr matrix\n")
      sampDistMat = read.csv(samp.dist.mat.path, header = TRUE, row.names = 1);
      png(result.path, width = 1200, height = 900);
      heatmap.2(x = as.matrix(datafile1),
                Rowv = TRUE,
                Colv = "Rowv",
                distfun = function(x) {f=x;return(as.dist(sampDistMat))},
                hclustfun = function(x){hclust(x, method = "complete")},
                dendrogram = "row",
                col = hmColAlt,
                symm = TRUE,
                trace = "none",
                margins = c(10,30),
                labCol = FALSE,
                labRow = showRowNames,
                keysize = 0.6,
                key.xlab = "Pearson Dist",
                density.info = "none",
                main = "Sample Pearson Correlation Heatmap");
    } else{
      cat("No extra variable\ngene expression matrix\n")
      sampDistMat = read.csv(samp.dist.mat.path, header = TRUE, row.names = 1);
      geneDistMat = read.csv(gene.dist.mat.path, header = TRUE, row.names = 1);
      sampDend = as.dendrogram(hclust(as.dist(sampDistMat), method = "complete"), hang = -1, check = TRUE);
      geneDend = as.dendrogram(hclust(as.dist(geneDistMat), method = "complete"), hang = -1, check = TRUE);
      png(result.path, width = 1200, height = 900);
      heatmap.2(x = as.matrix(datafile1),
                Rowv = sampDend,
                Colv = geneDend,
                col = hmCol,
                symm = FALSE,
                trace = "none",
                margins = c(10,30),
                labCol = FALSE,
                labRow = showRowNames,
                keysize = 0.6,
                key.xlab = distance.metric,
                density.info = "none",
                main = paste("Gene Expression and clustering (", distance.metric, "; ",isNormalized, ")", sep = ""));
    }
  }
  dev.off();
  cat(paste("Heatmap script finished. Files temporarilly saved at ", result.path, sep = ""));
}

tryCatch(runHeatmap(matrix.path = matrixPath, 
                    has.covariates = hasCovariates,
                    covariate.name = covariateName,
                    samp.dist.mat.path = sampDistMatPath,
                    gene.dist.mat.path = geneDistMatPath,
                    is.symcor = isSymCor,
                    result.path = resultPath,
                    distance.metric = distanceMetric,
                    is.categorical = isCategorical,
                    hmCol = blueBlackYellow,
                    hmColAlt = yellowBlackBlue), error = function(e) e)
sink()

# matrix.path = matrixPath
# has.covariates = hasCovariates
# covariate.name = covariateName
# samp.dist.mat.path = sampDistMatPath
# gene.dist.mat.path = geneDistMatPath
# is.symcor = isSymCor
# result.path = resultPath
# distance.metric = distanceMetric
# is.categorical = isCategorical
# hmCol = blueBlackYellow(100)
# hmColAlt = yellowBlackBlue(100)