function correctExam() {
  const bubbleSheetFiles = document.getElementById("bubble-sheet-files").files;
  const excelFile = document.getElementById("excel-file").files[0];

  // Validate inputs
  if (bubbleSheetFiles.length === 0) {
    alert("Please upload at least one bubble sheet file (PDF or images).");
    return;
  }
  if (!excelFile) {
    alert("Please upload the Excel file with correct answers.");
    return;
  }

  // Create FormData to send files
  const formData = new FormData();
  for (let i = 0; i < bubbleSheetFiles.length; i++) {
    formData.append("bubbleSheetFiles", bubbleSheetFiles[i]);
  }
  formData.append("excelFile", excelFile);

  // Show loading message
  const status = document.getElementById("status");
  if (status) {
    status.textContent = "Correcting exams, please wait...";
  }

  // Send request to Backend
  fetch("https://localhost:7181/api/BubbleSheetProcessor/CorrectBubbleSheets", {
    method: "POST",
    body: formData,
  })
    .then((response) => {
      if (!response.ok) {
        return response.text().then((text) => {
          let errorMessage = "Failed to correct exams.";
          try {
            const json = JSON.parse(text);
            errorMessage = json.message || errorMessage;
          } catch (e) {
            if (text) errorMessage = text;
          }
          throw new Error(errorMessage);
        });
      }
      return response.blob();
    })
    .then((blob) => {
      // Download the resulting Excel file
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "grades.xlsx";
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
      if (status) {
        status.textContent = "Correction completed successfully!";
      }
    })
    .catch((error) => {
      console.error("Error:", error);
      if (status) {
        status.textContent = `Error: ${error.message}`;
      }
      alert(`Error correcting exams: ${error.message}`);
    });
}
