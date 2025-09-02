const Materials = {
  initialize: function () {
    const materialsLabel = document.getElementById("materials-label");
    const materialsList = document.getElementById("materials-list");
    const toggleArrow = document.querySelector(".materials-toggle");

    materialsLabel.addEventListener("contextmenu", function (e) {
      e.preventDefault();
      Materials.showMaterialsLabelContextMenu(e);
    });

    toggleArrow.addEventListener("click", (e) => {
      e.stopPropagation();
      Materials.toggleMaterialsList();
    });

    materialsLabel.addEventListener("click", (e) => {
      e.stopPropagation();
      Materials.toggleMaterialsList();
    });

    Materials.clearContent();
  },

  showMaterialsLabelContextMenu: function (event) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let materialsLabelContextMenu = document.getElementById(
      "materialsLabelContextMenu"
    );

    if (!materialsLabelContextMenu) {
      materialsLabelContextMenu = document.createElement("ul");
      materialsLabelContextMenu.id = "materialsLabelContextMenu";
      materialsLabelContextMenu.className = "context-menu";
      materialsLabelContextMenu.style.position = "absolute";
      materialsLabelContextMenu.style.display = "none";
      materialsLabelContextMenu.style.zIndex = "1000";
      materialsLabelContextMenu.innerHTML = `<li id="materialAddOption">Add Material</li>`;
      document.body.appendChild(materialsLabelContextMenu);
    }

    materialsLabelContextMenu.style.left = `${event.pageX}px`;
    materialsLabelContextMenu.style.top = `${event.pageY}px`;
    materialsLabelContextMenu.style.display = "block";

    const newMaterialsLabelContextMenu =
      materialsLabelContextMenu.cloneNode(true);
    materialsLabelContextMenu.parentNode.replaceChild(
      newMaterialsLabelContextMenu,
      materialsLabelContextMenu
    );

    newMaterialsLabelContextMenu
      .querySelector("#materialAddOption")
      .addEventListener("click", () => {
        Materials.showAddForm();
        newMaterialsLabelContextMenu.style.display = "none";
      });

    document.addEventListener(
      "click",
      function hideMaterialsLabelContextMenu(e) {
        if (!newMaterialsLabelContextMenu.contains(e.target)) {
          newMaterialsLabelContextMenu.style.display = "none";
          document.removeEventListener("click", hideMaterialsLabelContextMenu);
        }
      }
    );
  },

  toggleMaterialsList: function () {
    const materialsList = document.getElementById("materials-list");
    const toggleArrow = document.querySelector(".materials-toggle");

    if (
      materialsList.style.display === "none" ||
      materialsList.style.display === ""
    ) {
      Materials.fetchMaterials();
      materialsList.style.display = "block";
      toggleArrow.innerHTML = "▲";
      toggleArrow.classList.add("expanded");
    } else {
      materialsList.style.display = "none";
      toggleArrow.innerHTML = "▼";
      toggleArrow.classList.remove("expanded");
    }
  },

  fetchMaterials: function () {
    const materialsList = document.getElementById("materials-list");
    materialsList.innerHTML =
      '<li class="loading-item"><i class="loading-spinner"></i> Loading...</li>';

    fetch("https://localhost:7181/api/Materials", {
      headers: {
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then((materials) => {
        materialsList.innerHTML = "";
        if (materials && materials.length > 0) {
          materials.forEach((material, index) => {
            const li = document.createElement("li");
            li.className = "material-item";
            li.innerHTML = `
                <div class="material-content">
                  <span class="material-name">${material.materialName}</span>
                  <span class="material-code">${
                    material.materialCode || ""
                  }</span>
                </div>
              `;
            li.style.animationDelay = `${index * 0.1}s`;

            li.addEventListener("contextmenu", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Materials.showMaterialContextMenu(e, material);
            });

            li.addEventListener("dblclick", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Materials.showDetailsForm(material);
            });

            li.addEventListener("click", (e) => {
              e.stopPropagation();
            });

            materialsList.appendChild(li);
          });
        } else {
          materialsList.innerHTML =
            '<li class="no-materials">No materials available</li>';
        }
      })
      .catch((err) => {
        console.error("Error fetching materials:", err);
        materialsList.innerHTML = `<li class="error-item">Error loading materials: ${err.message}</li>`;
      });
  },

  fetchDoctors: async function () {
    try {
      const response = await fetch("https://localhost:7181/api/Doctors", {
        headers: {
          Authorization: `Bearer ${localStorage.getItem("token")}`,
        },
      });
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (err) {
      console.error("Error fetching doctors:", err);
      return [];
    }
  },

  showMaterialContextMenu: function (event, material) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let materialContextMenu = document.getElementById("materialContextMenu");

    if (!materialContextMenu) {
      materialContextMenu = document.createElement("ul");
      materialContextMenu.id = "materialContextMenu";
      materialContextMenu.className = "context-menu";
      materialContextMenu.style.position = "absolute";
      materialContextMenu.style.display = "none";
      materialContextMenu.style.zIndex = "1000";
      materialContextMenu.innerHTML = `
          <li id="materialDetailsOption">View Details</li>
          <li id="materialEditOption">Edit</li>
          <li id="materialDeleteOption">Delete</li>
        `;
      document.body.appendChild(materialContextMenu);
    }

    materialContextMenu.style.left = `${event.pageX}px`;
    materialContextMenu.style.top = `${event.pageY}px`;
    materialContextMenu.style.display = "block";

    const newMaterialContextMenu = materialContextMenu.cloneNode(true);
    materialContextMenu.parentNode.replaceChild(
      newMaterialContextMenu,
      materialContextMenu
    );

    newMaterialContextMenu
      .querySelector("#materialDetailsOption")
      .addEventListener("click", () => {
        Materials.showDetailsForm(material);
        newMaterialContextMenu.style.display = "none";
      });

    newMaterialContextMenu
      .querySelector("#materialEditOption")
      .addEventListener("click", () => {
        Materials.showEditForm(material);
        newMaterialContextMenu.style.display = "none";
      });

    newMaterialContextMenu
      .querySelector("#materialDeleteOption")
      .addEventListener("click", () => {
        Materials.showDeleteForm(material);
        newMaterialContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideMaterialContextMenu(e) {
      if (!newMaterialContextMenu.contains(e.target)) {
        newMaterialContextMenu.style.display = "none";
        document.removeEventListener("click", hideMaterialContextMenu);
      }
    });
  },

  showDetailsForm: function (material) {
    // Fetch the latest material data before showing details
    fetch(`https://localhost:7181/api/Materials/${material.materialId}`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then((updatedMaterial) => {
        const content = document.getElementById("content");
        content.innerHTML = `
            <div class="content-display">
              <h3 class="content-title">Material Details</h3>
              <div class="material-details">
                <div class="detail-item">
                  <span class="detail-label">Material Name:</span>
                  <span class="detail-value">${
                    updatedMaterial.materialName
                  }</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">Material Code:</span>
                  <span class="detail-value">${
                    updatedMaterial.materialCode || "N/A"
                  }</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">Level:</span>
                  <span class="detail-value">${
                    updatedMaterial.level || "N/A"
                  }</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">Department:</span>
                  <span class="detail-value">${
                    updatedMaterial.department || "N/A"
                  }</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">Term:</span>
                  <span class="detail-value">${
                    updatedMaterial.term || "N/A"
                  }</span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">Doctor:</span>
                  <span class="detail-value">${
                    updatedMaterial.doctorName || "No Doctor Assigned"
                  }</span>
                </div>
              </div>
              <div class="button-group">
                <button type="button" class="btn-cancel" onclick="Materials.clearContent()">Close</button>
              </div>
            </div>
          `;
      })
      .catch((err) => {
        console.error("Error fetching material details:", err);
        Materials.showErrorMessage(
          "Error loading material details. Please try again."
        );
      });
  },

  showEditForm: async function (material) {
    const doctorId = localStorage.getItem("doctorId");
    if (!doctorId) {
      Materials.showErrorMessage("No logged-in doctor found. Please log in.");
      return;
    }
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Edit Material</h3>
          <form id="editMaterialForm" class="material-form">
            <div class="input-item">
              <label for="MaterialName">Material Name</label>
              <input type="text" id="MaterialName" value="${
                material.materialName
              }" required maxlength="100" />
            </div>
            <div class="input-item">
              <label for="MaterialCode">Material Code</label>
              <input type="text" id="MaterialCode" value="${
                material.materialCode || ""
              }" required maxlength="20" />
            </div>
            <div class="input-item">
              <label for="Level">Level</label>
              <select id="Level" required>
                <option value="First" ${
                  material.level === "First" ? "selected" : ""
                }>First</option>
                <option value="Second" ${
                  material.level === "Second" ? "selected" : ""
                }>Second</option>
                <option value="Third" ${
                  material.level === "Third" ? "selected" : ""
                }>Third</option>
                <option value="Fourth" ${
                  material.level === "Fourth" ? "selected" : ""
                }>Fourth</option>
                <option value="Fifth" ${
                  material.level === "Fifth" ? "selected" : ""
                }>Fifth</option>
              </select>
            </div>
            <div class="input-item">
              <label for="Department">Department</label>
              <input type="text" id="Department" value="${
                material.department || ""
              }" required maxlength="100" />
            </div>
            <div class="input-item">
              <label for="Term">Term</label>
              <select id="Term" required>
                <option value="1" ${
                  material.term === 1 ? "selected" : ""
                }>Term 1</option>
                <option value="2" ${
                  material.term === 2 ? "selected" : ""
                }>Term 2</option>
              </select>
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Changes</button>
              <button type="button" class="btn-cancel" onclick="Materials.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;
    document
      .getElementById("editMaterialForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();
        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;
        const data = {
          materialName: document.getElementById("MaterialName").value,
          materialCode: document.getElementById("MaterialCode").value,
          level: document.getElementById("Level").value,
          department: document.getElementById("Department").value,
          term: parseInt(document.getElementById("Term").value),
        };
        fetch(`https://localhost:7181/api/Materials/${material.materialId}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${localStorage.getItem("token")}`,
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
            Materials.showSuccessMessage(response.message);
            Materials.fetchMaterials();
            Materials.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            Materials.showErrorMessage(
              "Error updating material. Please try again."
            );
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },
  showDeleteForm: function (material) {
    const content = document.getElementById("content");
    content.innerHTML = `
      <div class="content-display">
        <h3 class="content-title">Delete Material</h3>
        <p class="delete-message">
          Are you sure you want to delete the material 
          <strong>${material.materialName}</strong>?
        </p>
        <div class="button-group">
          <button type="button" class="btn-delete" onclick="Materials.deleteMaterial(${material.materialId})">Delete</button>
          <button type="button" class="btn-cancel" onclick="Materials.clearContent()">Cancel</button>
        </div>
      </div>
    `;
  },

  deleteMaterial: function (materialId) {
    const deleteBtn = document.querySelector(".btn-delete");
    const originalText = deleteBtn.textContent;
    deleteBtn.innerHTML = '<i class="loading-spinner"></i> Deleting...';
    deleteBtn.disabled = true;

    fetch(`https://localhost:7181/api/Materials/${materialId}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
      },
    })
      .then((res) => {
        if (!res.ok) {
          throw new Error(`HTTP error! status: ${res.status}`);
        }
        return res.json();
      })
      .then((response) => {
        Materials.showSuccessMessage(response.message);
        Materials.fetchMaterials();
        Materials.clearContent();
      })
      .catch((err) => {
        console.error("Error:", err);
        Materials.showErrorMessage(
          "Error deleting material. Please try again."
        );
      })
      .finally(() => {
        deleteBtn.textContent = originalText;
        deleteBtn.disabled = false;
      });
  },

  showAddForm: async function () {
    const doctorId = localStorage.getItem("doctorId");
    if (!doctorId) {
      Materials.showErrorMessage("No logged-in doctor found. Please log in.");
      return;
    }
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Add New Material</h3>
          <form id="materialForm" class="material-form">
            <div class="input-item">
              <label for="MaterialName">Material Name</label>
              <input type="text" id="MaterialName" placeholder="Enter material name" required maxlength="100" />
            </div>
            <div class="input-item">
              <label for="MaterialCode">Material Code</label>
              <input type="text" id="MaterialCode" placeholder="Enter material code" required maxlength="20" />
            </div>
            <div class="input-item">
              <label for="Level">Level</label>
              <select id="Level" required>
                <option value="">Select a level</option>
                <option value="First">First</option>
                <option value="Second">Second</option>
                <option value="Third">Third</option>
                <option value="Fourth">Fourth</option>
                <option value="Fifth">Fifth</option>
              </select>
            </div>
            <div class="input-item">
              <label for="Department">Department</label>
              <input type="text" id="Department" placeholder="Enter department" required maxlength="100" />
            </div>
            <div class="input-item">
              <label for="Term">Term</label>
              <select id="Term" required>
                <option value="">Select a term</option>
                <option value="1">Term 1</option>
                <option value="2">Term 2</option>
              </select>
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Material</button>
              <button type="button" class="btn-cancel" onclick="Materials.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;
    document
      .getElementById("materialForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();
        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;
        const data = {
          materialName: document.getElementById("MaterialName").value,
          materialCode: document.getElementById("MaterialCode").value,
          level: document.getElementById("Level").value,
          department: document.getElementById("Department").value,
          term: parseInt(document.getElementById("Term").value),
        };
        fetch("https://localhost:7181/api/Materials", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${localStorage.getItem("token")}`,
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
            const material = {
              materialId: response.materialId,
              materialName: data.materialName,
              materialCode: data.materialCode,
            };
            Materials.showSuccessMessage(response.message);
            Materials.addMaterialToTree(material);
            Materials.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            Materials.showErrorMessage(
              "Error adding material. Please try again."
            );
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  addMaterialToTree: function (material) {
    const materialsList = document.getElementById("materials-list");
    const li = document.createElement("li");
    li.className = "material-item new-item";
    li.innerHTML = `
        <div class="material-content">
          <span class="material-name">${material.materialName}</span>
          <span class="material-code">${material.materialCode || ""}</span>
        </div>
      `;
    li.addEventListener("contextmenu", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Materials.showMaterialContextMenu(e, material);
    });
    li.addEventListener("dblclick", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Materials.showDetailsForm(material);
    });
    li.addEventListener("click", (e) => {
      e.stopPropagation();
    });
    materialsList.appendChild(li);
    setTimeout(() => {
      li.classList.remove("new-item");
    }, 1000);
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
            <button onclick="Materials.clearContent()" class="btn-back">Return to Home</button>
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
            <button onclick="Materials.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
  },
};

document.addEventListener("DOMContentLoaded", Materials.initialize);
