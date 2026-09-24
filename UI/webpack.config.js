const path = require("path");
const MOD = require("./mod.json");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const TerserPlugin = require("terser-webpack-plugin");

const CSII_USERDATAPATH = process.env.CSII_USERDATAPATH;
if (!CSII_USERDATAPATH) {
  throw new Error("CSII_USERDATAPATH environment variable is not set; install the CS2 Modding Toolchain or set it explicitly.");
}

const outputDir = path.resolve(CSII_USERDATAPATH, "Mods", MOD.id);
const banner = `\n * Cities: Skylines II UI Module\n *\n * Id: ${MOD.id}\n * Author: ${MOD.author}\n * Version: ${MOD.version}\n * Dependencies: ${MOD.dependencies.join(",")}\n`;

module.exports = {
  mode: "production",
  stats: "errors-warnings",
  entry: { [MOD.id]: "./src/index.tsx" },
  externalsType: "window",
  externals: {
    react: "React",
    "react-dom": "ReactDOM",
    "cs2/modding": "cs2/modding",
    "cs2/api": "cs2/api",
    "cs2/bindings": "cs2/bindings",
    "cs2/l10n": "cs2/l10n",
    "cs2/ui": "cs2/ui",
    "cs2/input": "cs2/input",
    "cs2/utils": "cs2/utils",
    "cohtml/cohtml": "cohtml/cohtml"
  },
  module: {
    rules: [
      { test: /\.tsx?$/, use: "ts-loader", exclude: /node_modules/ },
      {
        test: /\.s?css$/,
        include: path.join(__dirname, "src"),
        use: [
          MiniCssExtractPlugin.loader,
          { loader: "css-loader", options: { modules: { auto: true, exportLocalsConvention: "camelCase" } } },
          "sass-loader"
        ]
      }
    ]
  },
  resolve: {
    extensions: [".tsx", ".ts", ".js"],
    modules: ["node_modules", path.join(__dirname, "src")]
  },
  output: {
    path: outputDir,
    library: { type: "module" },
    publicPath: "coui://ui-mods/"
  },
  optimization: {
    minimize: true,
    minimizer: [new TerserPlugin({ extractComments: { banner: () => banner } })]
  },
  experiments: { outputModule: true },
  plugins: [new MiniCssExtractPlugin()]
};
