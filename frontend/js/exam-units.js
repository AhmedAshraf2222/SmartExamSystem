const ExamUnits = {
  initialize: function () {
    const examUnitsLabel = document.getElementById("exam-units-label");
    const examUnitsList = document.getElementById("exam-units-list");
    const toggleArrow = document.querySelector(".exam-units-toggle");

    examUnitsLabel.addEventListener("contextmenu", function (e) {
      e.preventDefault();
      ExamUnits.showExamUnitsLabelContextMenu(e);
    });

    toggleArrow.addEventListener("click", (e) => {
      e.stopPropagation();
      ExamUnits.toggleExamUnitsList();
    });

    examUnitsLabel.addEventListener("click", (e) => {
      e.stopPropagation();
      ExamUnits.toggleExamUnitsList();
    });

    ExamUnits.clearContent();
  },

  showExamUnitsLabelContextMenu: function (event) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let examUnitsLabelContextMenu = document.getElementById(
      "examUnitsLabelContextMenu"
    );

    if (!examUnitsLabelContextMenu) {
      examUnitsLabelContextMenu = document.createElement("ul");
      examUnitsLabelContextMenu.id = "examUnitsLabelContextMenu";
      examUnitsLabelContextMenu.className = "context-menu";
      examUnitsLabelContextMenu.style.position = "absolute";
      examUnitsLabelContextMenu.style.zIndex = "1000";
      examUnitsLabelContextMenu.innerHTML = `
        <li id="examUnitAddOption">Add Exam Unit</li>
      `;
      document.body.appendChild(examUnitsLabelContextMenu);

      examUnitsLabelContextMenu
        .querySelector("#examUnitAddOption")
        .addEventListener("click", () => {
          ExamUnits.showAddForm();
          examUnitsLabelContextMenu.style.display = "none";
        });
    }

    const menuHeight = examUnitsLabelContextMenu.offsetHeight;
    const windowHeight = window.innerHeight;

    let top = event.pageY;
    let left = event.pageX;

    if (top + menuHeight > windowHeight) {
      top -= menuHeight;
    }

    examUnitsLabelContextMenu.style.left = `${left}px`;
    examUnitsLabelContextMenu.style.top = `${top}px`;
    examUnitsLabelContextMenu.style.display = "block";

    document.addEventListener(
      "click",
      function hideExamUnitsLabelContextMenu(e) {
        if (!examUnitsLabelContextMenu.contains(e.target)) {
          examUnitsLabelContextMenu.style.display = "none";
          document.removeEventListener("click", hideExamUnitsLabelContextMenu);
        }
      }
    );
  },

  toggleExamUnitsList: function () {
    const examUnitsList = document.getElementById("exam-units-list");
    const toggleArrow = document.querySelector(".exam-units-toggle");

    if (
      examUnitsList.style.display === "none" ||
      examUnitsList.style.display === ""
    ) {
      ExamUnits.fetchExamUnits();
      examUnitsList.style.display = "block";
      toggleArrow.innerHTML = "▲";
      toggleArrow.classList.add("expanded");
    } else {
      examUnitsList.style.display = "none";
      toggleArrow.innerHTML = "▼";
      toggleArrow.classList.remove("expanded");
    }
  },

  fetchExamUnits: function () {
    const examUnitsList = document.getElementById("exam-units-list");
    examUnitsList.innerHTML =
      '<li class="loading-item"><i class="loading-spinner"></i> Loading...</li>';

    fetch("https://localhost:7181/api/ExamUnits", {
      headers: {},
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then((response) => {
        examUnitsList.innerHTML = "";
        const examUnits = response.data;
        if (examUnits && examUnits.length > 0) {
          examUnits.forEach((examUnit, index) => {
            const li = document.createElement("li");
            li.className = "exam-unit-item";
            li.innerHTML = `
                <div class="material-content">
                  <span class="material-name">Unit Order: ${
                    examUnit.unitOrder
                  }</span>
                  <span class="material-code">Exam: ${
                    examUnit.examName || "N/A"
                  } | Group: ${examUnit.groupName || "N/A"}</span>
                </div>
              `;
            li.style.animationDelay = `${index * 0.1}s`;

            li.addEventListener("contextmenu", (e) => {
              e.preventDefault();
              e.stopPropagation();
              ExamUnits.showExamUnitContextMenu(e, examUnit);
            });

            li.addEventListener("dblclick", (e) => {
              e.preventDefault();
              e.stopPropagation();
              ExamUnits.showDetailsForm(examUnit);
            });

            li.addEventListener("click", (e) => {
              e.stopPropagation();
            });

            examUnitsList.appendChild(li);
          });
        } else {
          examUnitsList.innerHTML =
            '<li class="no-materials">No exam units available</li>';
        }
      })
      .catch((err) => {
        console.error("Error fetching exam units:", err);
        examUnitsList.innerHTML = `<li class="error-item">Error loading exam units: ${err.message}</li>`;
      });
  },

  fetchExams: async function () {
    try {
      const response = await fetch("https://localhost:7181/api/Exams", {
        headers: {},
      });
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (err) {
      console.error("Error fetching exams:", err);
      return [];
    }
  },

  fetchGroups: async function () {
    try {
      const response = await fetch("https://localhost:7181/api/Groups", {
        headers: {},
      });
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (err) {
      console.error("Error fetching groups:", err);
      return [];
    }
  },

  showExamUnitContextMenu: function (event, examUnit) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let examUnitContextMenu = document.getElementById("examUnitContextMenu");

    if (!examUnitContextMenu) {
      examUnitContextMenu = document.createElement("ul");
      examUnitContextMenu.id = "examUnitContextMenu";
      examUnitContextMenu.className = "context-menu";
      examUnitContextMenu.style.position = "absolute";
      examUnitContextMenu.style.display = "none";
      examUnitContextMenu.style.zIndex = "1000";
      examUnitContextMenu.innerHTML = `
          <li id="examUnitDetailsOption">View Details</li>
          <li id="examUnitEditOption">Edit</li>
          <li id="examUnitDeleteOption">Delete</li>
        `;
      document.body.appendChild(examUnitContextMenu);
    }

    examUnitContextMenu.style.left = `${event.pageX}px`;
    examUnitContextMenu.style.top = `${event.pageY}px`;
    examUnitContextMenu.style.display = "block";

    const newExamUnitContextMenu = examUnitContextMenu.cloneNode(true);
    examUnitContextMenu.parentNode.replaceChild(
      newExamUnitContextMenu,
      examUnitContextMenu
    );

    newExamUnitContextMenu
      .querySelector("#examUnitDetailsOption")
      .addEventListener("click", () => {
        ExamUnits.showDetailsForm(examUnit);
        newExamUnitContextMenu.style.display = "none";
      });

    newExamUnitContextMenu
      .querySelector("#examUnitEditOption")
      .addEventListener("click", () => {
        ExamUnits.showEditForm(examUnit);
        newExamUnitContextMenu.style.display = "none";
      });

    newExamUnitContextMenu
      .querySelector("#examUnitDeleteOption")
      .addEventListener("click", () => {
        ExamUnits.showDeleteForm(examUnit);
        newExamUnitContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideExamUnitContextMenu(e) {
      if (!newExamUnitContextMenu.contains(e.target)) {
        newExamUnitContextMenu.style.display = "none";
        document.removeEventListener("click", hideExamUnitContextMenu);
      }
    });
  },

  showDetailsForm: function (examUnit) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Exam Unit Details</h3>
          <div class="material-details">
            <div class="detail-item">
              <span class="detail-label">Unit Order:</span>
              <span class="detail-value">${examUnit.unitOrder}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Exam:</span>
              <span class="detail-value">${examUnit.examName || "N/A"}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Group:</span>
              <span class="detail-value">${examUnit.groupName || "N/A"}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Main Degree:</span>
              <span class="detail-value">${examUnit.mainDegree}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Total Problems:</span>
              <span class="detail-value">${examUnit.totalProblems}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Shuffle:</span>
              <span class="detail-value">${
                examUnit.shuffle ? "Yes" : "No"
              }</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">All Problems:</span>
              <span class="detail-value">${examUnit.allProblems}</span>
            </div>
            <div class="button-group">
              <button type="button" class="btn-save" onclick="ExamUnits.showGenerateFilesForm(${
                examUnit.examId
              })">Generate Exam Files</button>
              <button type="button" class="btn-cancel" onclick="ExamUnits.clearContent()">Close</button>
            </div>
          </div>
        </div>
      `;
  },

  showEditForm: async function (examUnit) {
    const exams = await ExamUnits.fetchExams();
    const groups = await ExamUnits.fetchGroups();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Edit Exam Unit</h3>
          <form id="editExamUnitForm" class="material-form">
            <div class="input-item">
              <label for="ExamId">Exam</label>
              <select id="ExamId" required>
                <option value="">Select an exam</option>
                ${exams
                  .map(
                    (exam) =>
                      `<option value="${exam.examId}" ${
                        exam.examId === examUnit.examId ? "selected" : ""
                      }>${exam.examName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="GroupId">Group</label>
              <select id="GroupId" required>
                <option value="">Select a group</option>
                ${groups
                  .map(
                    (group) =>
                      `<option value="${group.groupId}" ${
                        group.groupId === examUnit.groupId ? "selected" : ""
                      }>${group.groupName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="MainDegree">Main Degree</label>
              <input type="number" id="MainDegree" value="${
                examUnit.mainDegree
              }" required min="0" />
            </div>
            <div class="input-item">
              <label for="TotalProblems">Total Problems</label>
              <input type="number" id="TotalProblems" value="${
                examUnit.totalProblems
              }" required min="0" />
            </div>
            <div class="input-item checkbox">
              <label for="Shuffle">Shuffle</label>
              <input type="checkbox" id="Shuffle" ${
                examUnit.shuffle ? "checked" : ""
              } />
            </div>
            <div class="input-item">
              <label for="AllProblems">All Problems</label>
              <input type="number" id="AllProblems" value="${
                examUnit.allProblems
              }" required min="0" />
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Changes</button>
              <button type="button" class="btn-cancel" onclick="ExamUnits.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    document
      .getElementById("editExamUnitForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const data = {
          examId: parseInt(document.getElementById("ExamId").value),
          groupId: parseInt(document.getElementById("GroupId").value),
          mainDegree: parseInt(document.getElementById("MainDegree").value),
          totalProblems: parseInt(
            document.getElementById("TotalProblems").value
          ),
          shuffle: document.getElementById("Shuffle").checked,
          allProblems: parseInt(document.getElementById("AllProblems").value),
        };

        fetch(`https://localhost:7181/api/ExamUnits/${examUnit.unitOrder}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(data),
        })
          .then((res) => {
            if (!res.ok) {
              throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.json();
          })
          .then((response) => {
            ExamUnits.showSuccessMessage(response.message);
            ExamUnits.fetchExamUnits();
            ExamUnits.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            ExamUnits.showErrorMessage(
              "Error updating exam unit. Please try again."
            );
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  showDeleteForm: function (examUnit) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Delete Exam Unit</h3>
          <p class="delete-message">Are you sure you want to delete the exam unit with Unit Order <strong>${examUnit.unitOrder}</strong>?</p>
          <div class="button-group">
            <button type="button" class="btn-delete" onclick="ExamUnits.deleteExamUnit(${examUnit.unitOrder})">Delete</button>
            <button type="button" class="btn-cancel" onclick="ExamUnits.clearContent()">Cancel</button>
          </div>
        </div>
      `;
  },

  deleteExamUnit: function (unitOrder) {
    const deleteBtn = document.querySelector(".btn-delete");
    const originalText = deleteBtn.textContent;
    deleteBtn.innerHTML = '<i class="loading-spinner"></i> Deleting...';
    deleteBtn.disabled = true;

    fetch(`https://localhost:7181/api/ExamUnits/${unitOrder}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((res) => {
        if (!res.ok) {
          throw new Error(`HTTP error! status: ${res.status}`);
        }
        return res.json();
      })
      .then((response) => {
        ExamUnits.showSuccessMessage(response.message);
        ExamUnits.fetchExamUnits();
        ExamUnits.clearContent();
      })
      .catch((err) => {
        console.error("Error:", err);
        ExamUnits.showErrorMessage(
          "Error deleting exam unit. Please try again."
        );
      })
      .finally(() => {
        deleteBtn.textContent = originalText;
        deleteBtn.disabled = false;
      });
  },

  showAddForm: async function () {
    const exams = await ExamUnits.fetchExams();
    const groups = await ExamUnits.fetchGroups();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Add New Exam Unit</h3>
          <form id="examUnitForm" class="material-form">
            <div class="input-item">
              <label for="ExamId">Exam</label>
              <select id="ExamId" required>
                <option value="">Select an exam</option>
                ${exams
                  .map(
                    (exam) =>
                      `<option value="${exam.examId}">${exam.examName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="GroupId">Group</label>
              <select id="GroupId" required>
                <option value="">Select a group</option>
                ${groups
                  .map(
                    (group) =>
                      `<option value="${group.groupId}">${group.groupName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="MainDegree">Main Degree</label>
              <input type="number" id="MainDegree" placeholder="Enter main degree" required min="0" />
            </div>
            <div class="input-item">
              <label for="TotalProblems">Total Problems</label>
              <input type="number" id="TotalProblems" placeholder="Enter total problems" required min="0" />
            </div>
            <div class="input-item checkbox">
              <label for="Shuffle">Shuffle</label>
              <input type="checkbox" id="Shuffle" />
            </div>
            <div class="input-item">
              <label for="AllProblems">All Problems</label>
              <input type="number" id="AllProblems" placeholder="Enter all problems" required min="0" />
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Exam Unit</button>
              <button type="button" class="btn-cancel" onclick="ExamUnits.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    document
      .getElementById("examUnitForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const data = {
          examId: parseInt(document.getElementById("ExamId").value),
          groupId: parseInt(document.getElementById("GroupId").value),
          mainDegree: parseInt(document.getElementById("MainDegree").value),
          totalProblems: parseInt(
            document.getElementById("TotalProblems").value
          ),
          shuffle: document.getElementById("Shuffle").checked,
          allProblems: parseInt(document.getElementById("AllProblems").value),
        };

        fetch("https://localhost:7181/api/ExamUnits", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(data),
        })
          .then((res) => {
            if (!res.ok) {
              throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.json();
          })
          .then((response) => {
            ExamUnits.showSuccessMessage(response.message);
            ExamUnits.addExamUnitToTree(response.data);
            ExamUnits.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            ExamUnits.showErrorMessage(
              "Error adding exam unit. Please try again."
            );
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  addExamUnitToTree: function (examUnit) {
    const examUnitsList = document.getElementById("exam-units-list");
    const li = document.createElement("li");
    li.className = "exam-unit-item new-item";
    li.innerHTML = `
        <div class="material-content">
          <span class="material-name">Unit Order: ${examUnit.unitOrder}</span>
          <span class="material-code">Exam: ${
            examUnit.examName || "N/A"
          } | Group: ${examUnit.groupName || "N/A"}</span>
        </div>
      `;

    li.addEventListener("contextmenu", (e) => {
      e.preventDefault();
      e.stopPropagation();
      ExamUnits.showExamUnitContextMenu(e, examUnit);
    });

    li.addEventListener("dblclick", (e) => {
      e.preventDefault();
      e.stopPropagation();
      ExamUnits.showDetailsForm(examUnit);
    });

    li.addEventListener("click", (e) => {
      e.stopPropagation();
    });

    examUnitsList.appendChild(li);
    setTimeout(() => {
      li.classList.remove("new-item");
    }, 1000);
  },

  showGenerateFilesForm: function (examId) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Generate Exam Files</h3>
          <form id="generateFilesForm" class="material-form">
            <div class="input-item">
              <label for="NumberOfModels">Number of Models</label>
              <input type="number" id="NumberOfModels" placeholder="Enter number of models" required min="1" />
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Generate Files</button>
              <button type="button" class="btn-cancel" onclick="ExamUnits.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    document
      .getElementById("generateFilesForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Generating...';
        saveBtn.disabled = true;

        const numberOfModels = parseInt(
          document.getElementById("NumberOfModels").value
        );

        fetch(
          `https://localhost:7181/api/ExamUnits/GenerateExamFiles?id=${examId}&numberOfModels=${numberOfModels}`,
          {
            method: "POST",
            headers: {
              Accept: "application/zip",
            },
          }
        )
          .then((res) => {
            if (!res.ok) {
              throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.blob();
          })
          .then((blob) => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = `exam_models_${new Date()
              .toISOString()
              .slice(0, 19)
              .replace(/[:T]/g, "")}.zip`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url);
            ExamUnits.showSuccessMessage("Exam files generated successfully.");
            ExamUnits.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            ExamUnits.showErrorMessage(
              "Error generating exam files. Please try again."
            );
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  clearContent: function () {
    document.getElementById("content").innerHTML = `
        <div class="welcome-message">
          <h2>Welcome to Exams Management System</h2>
          <p>Select an item from the sidebar to begin</p>
        </div>
      `;
  },

  showSuccessMessage: function (message) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <div class="message-container success">
            <div class="message-icon">✓</div>
            <h3>${message}</h3>
            <button onclick="ExamUnits.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
  },

  showErrorMessage: function (message) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <div class="message-container error">
            <div class="message-icon">✗</div>
            <h3>${message}</h3>
            <button onclick="ExamUnits.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
  },
};

document.addEventListener("DOMContentLoaded", ExamUnits.initialize);
