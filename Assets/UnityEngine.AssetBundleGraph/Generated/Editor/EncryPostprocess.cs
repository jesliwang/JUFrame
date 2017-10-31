using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using UnityEngine.AssetBundles.GraphTool;

using JUFrame;

/**
Example code for asset bundle build postprocess.
*/
public class EncryPostprocess : IPostprocess {
	/* 
	 * DoPostprocess() is called when build performed.
	 * @param [in] reports	collection of AssetBundleBuildReport from each BundleBuilders.
	 */
	public void DoPostprocess (IEnumerable<AssetBundleBuildReport> buildReports, IEnumerable<ExportReport> exportReports) {

        StringBuilder versionString = new StringBuilder();
        versionString.AppendFormat("Version:xxxx\n");

        foreach (var report in buildReports) {
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("BUILD REPORT({0}):\n-------\n", report.Node.Name);

			foreach(var v in report.BuiltBundleFiles) {
                sb.AppendFormat("before->{0}:{1}\n", v.fileNameAndExtension, Convert.ToBase64String(SystemDataUtility.GetHash(v.absolutePath)));
                FileEncryUtility.EncryFile(v.absolutePath);
				sb.AppendFormat("after->{0}:{1}\n", v.fileNameAndExtension, Convert.ToBase64String( SystemDataUtility.GetHash(v.absolutePath) ) );


                versionString.AppendFormat("{0}:{1}\n", v.fileNameAndExtension, Convert.ToBase64String(SystemDataUtility.GetHash(v.absolutePath)));
            }

			sb.Append("-------\n");
			//Debug.Log(sb.ToString());
		}

        Debug.LogError(versionString.ToString());

	}
}
