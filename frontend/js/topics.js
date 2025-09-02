const Topics = {
  initialize: function () {
    const topicsLabel = document.getElementById("topics-label");
    const topicsList = document.getElementById("topics-list");
    const toggleArrow = document.querySelector(".topics-toggle");

    topicsLabel.addEventListener("contextmenu", function (e) {
      e.preventDefault();
      Topics.showTopicsLabelContextMenu(e);
    });

    toggleArrow.addEventListener("click", (e) => {
      e.stopPropagation();
      Topics.toggleTopicsList();
    });

    topicsLabel.addEventListener("click", (e) => {
      e.stopPropagation();
      Topics.toggleTopicsList();
    });

    Topics.clearContent();
  },

  showTopicsLabelContextMenu: function (event) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let topicsLabelContextMenu = document.getElementById(
      "topicsLabelContextMenu"
    );

    if (!topicsLabelContextMenu) {
      topicsLabelContextMenu = document.createElement("ul");
      topicsLabelContextMenu.id = "topicsLabelContextMenu";
      topicsLabelContextMenu.className = "context-menu";
      topicsLabelContextMenu.style.position = "absolute";
      topicsLabelContextMenu.style.display = "none";
      topicsLabelContextMenu.style.zIndex = "1000";
      topicsLabelContextMenu.innerHTML = `<li id="topicAddOption">Add Topic</li>`;
      document.body.appendChild(topicsLabelContextMenu);
    }

    topicsLabelContextMenu.style.left = `${event.pageX}px`;
    topicsLabelContextMenu.style.top = `${event.pageY}px`;
    topicsLabelContextMenu.style.display = "block";

    const newTopicsLabelContextMenu = topicsLabelContextMenu.cloneNode(true);
    topicsLabelContextMenu.parentNode.replaceChild(
      newTopicsLabelContextMenu,
      topicsLabelContextMenu
    );

    newTopicsLabelContextMenu
      .querySelector("#topicAddOption")
      .addEventListener("click", () => {
        Topics.showAddForm();
        newTopicsLabelContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideTopicsLabelContextMenu(e) {
      if (!newTopicsLabelContextMenu.contains(e.target)) {
        newTopicsLabelContextMenu.style.display = "none";
        document.removeEventListener("click", hideTopicsLabelContextMenu);
      }
    });
  },

  toggleTopicsList: function () {
    const topicsList = document.getElementById("topics-list");
    const toggleArrow = document.querySelector(".topics-toggle");

    if (
      topicsList.style.display === "none" ||
      topicsList.style.display === ""
    ) {
      Topics.fetchTopics();
      topicsList.style.display = "block";
      toggleArrow.innerHTML = "▲";
      toggleArrow.classList.add("expanded");
    } else {
      topicsList.style.display = "none";
      toggleArrow.innerHTML = "▼";
      toggleArrow.classList.remove("expanded");
    }
  },

  fetchTopics: async function () {
    const topicsList = document.getElementById("topics-list");
    topicsList.innerHTML =
      '<li class="loading-item"><i class="loading-spinner"></i> Loading...</li>';

    try {
      const [topicsResponse, materialsResponse] = await Promise.all([
        fetch("https://localhost:7181/api/Topics"),
        fetch("https://localhost:7181/api/Topics/materials"),
      ]);

      if (!topicsResponse.ok) {
        throw new Error(`HTTP error! status: ${topicsResponse.status}`);
      }
      if (!materialsResponse.ok) {
        throw new Error(`HTTP error! status: ${materialsResponse.status}`);
      }

      const topics = await topicsResponse.json();
      const materials = await materialsResponse.json();
      const materialMap = materials.reduce((map, material) => {
        map[material.materialId] = material.materialName;
        return map;
      }, {});

      topicsList.innerHTML = "";
      if (topics && topics.length > 0) {
        topics.forEach((topic, index) => {
          const li = document.createElement("li");
          li.className = "topic-item";
          li.innerHTML = `
              <div class="material-content">
                <span class="material-name">${topic.topicName}</span>
                <span class="material-code">Material: ${
                  materialMap[topic.materialId] || "N/A"
                }</span>
              </div>
            `;
          li.style.animationDelay = `${index * 0.1}s`;

          li.addEventListener("contextmenu", (e) => {
            e.preventDefault();
            e.stopPropagation();
            topic.materialName = materialMap[topic.materialId] || "N/A";
            Topics.showTopicContextMenu(e, topic);
          });

          li.addEventListener("dblclick", (e) => {
            e.preventDefault();
            e.stopPropagation();
            topic.materialName = materialMap[topic.materialId] || "N/A";
            Topics.showDetailsForm(topic);
          });

          li.addEventListener("click", (e) => {
            e.stopPropagation();
          });

          topicsList.appendChild(li);
        });
      } else {
        topicsList.innerHTML =
          '<li class="no-materials">No topics available</li>';
      }
    } catch (err) {
      console.error("Error fetching topics:", err);
      topicsList.innerHTML = `<li class="error-item">Error loading topics: ${err.message}</li>`;
    }
  },

  fetchMaterials: async function () {
    try {
      const response = await fetch(
        "https://localhost:7181/api/Topics/materials"
      );
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (err) {
      console.error("Error fetching materials:", err);
      return [];
    }
  },

  showTopicContextMenu: function (event, topic) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let topicContextMenu = document.getElementById("topicContextMenu");

    if (!topicContextMenu) {
      topicContextMenu = document.createElement("ul");
      topicContextMenu.id = "topicContextMenu";
      topicContextMenu.className = "context-menu";
      topicContextMenu.style.position = "absolute";
      topicContextMenu.style.display = "none";
      topicContextMenu.style.zIndex = "1000";
      topicContextMenu.innerHTML = `
          <li id="topicDetailsOption">View Details</li>
          <li id="topicEditOption">Edit</li>
          <li id="topicDeleteOption">Delete</li>
        `;
      document.body.appendChild(topicContextMenu);
    }

    topicContextMenu.style.left = `${event.pageX}px`;
    topicContextMenu.style.top = `${event.pageY}px`;
    topicContextMenu.style.display = "block";

    const newTopicContextMenu = topicContextMenu.cloneNode(true);
    topicContextMenu.parentNode.replaceChild(
      newTopicContextMenu,
      topicContextMenu
    );

    newTopicContextMenu
      .querySelector("#topicDetailsOption")
      .addEventListener("click", () => {
        Topics.showDetailsForm(topic);
        newTopicContextMenu.style.display = "none";
      });

    newTopicContextMenu
      .querySelector("#topicEditOption")
      .addEventListener("click", () => {
        Topics.showEditForm(topic);
        newTopicContextMenu.style.display = "none";
      });

    newTopicContextMenu
      .querySelector("#topicDeleteOption")
      .addEventListener("click", () => {
        Topics.showDeleteForm(topic);
        newTopicContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideTopicContextMenu(e) {
      if (!newTopicContextMenu.contains(e.target)) {
        newTopicContextMenu.style.display = "none";
        document.removeEventListener("click", hideTopicContextMenu);
      }
    });
  },

  showDetailsForm: function (topic) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Topic Details</h3>
          <div class="material-details">
            <div class="detail-item">
              <span class="detail-label">Topic Name:</span>
              <span class="detail-value">${topic.topicName}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Material:</span>
              <span class="detail-value">${topic.materialName || "N/A"}</span>
            </div>
          </div>
          <div class="button-group">
            <button type="button" class="btn-cancel" onclick="Topics.clearContent()">Close</button>
          </div>
        </div>
      `;
  },

  showEditForm: async function (topic) {
    const materials = await Topics.fetchMaterials();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Edit Topic</h3>
          <form id="editTopicForm" class="material-form">
            <div class="input-item">
              <label for="TopicName">Topic Name</label>
              <input type="text" id="TopicName" value="${
                topic.topicName
              }" required maxlength="40" />
            </div>
            <div class="input-item">
              <label for="MaterialId">Material</label>
              <select id="MaterialId" required>
                <option value="">Select a material</option>
                ${materials
                  .map(
                    (material) =>
                      `<option value="${material.materialId}" ${
                        material.materialId === topic.materialId
                          ? "selected"
                          : ""
                      }>${material.materialName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Changes</button>
              <button type="button" class="btn-cancel" onclick="Topics.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    document
      .getElementById("editTopicForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const data = {
          topicName: document.getElementById("TopicName").value,
          materialId: parseInt(document.getElementById("MaterialId").value),
        };

        fetch(`https://localhost:7181/api/Topics/${topic.topicId}`, {
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
            Topics.showSuccessMessage(response.message);
            Topics.fetchTopics();
            Topics.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            Topics.showErrorMessage("Error updating topic. Please try again.");
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  showDeleteForm: function (topic) {
    const content = document.getElementById("content");
    content.innerHTML = `
      <div class="content-display">
        <h3 class="content-title">Delete Topic</h3>
        <p class="delete-message">
          Are you sure you want to delete the topic 
          <strong>${topic.topicName}</strong>?
        </p>
        <div class="button-group">
          <button type="button" class="btn-delete" onclick="Topics.deleteTopic(${topic.topicId})">Delete</button>
          <button type="button" class="btn-cancel" onclick="Topics.clearContent()">Cancel</button>
        </div>
      </div>
    `;
  },

  deleteTopic: function (topicId) {
    const deleteBtn = document.querySelector(".btn-delete");
    const originalText = deleteBtn.textContent;
    deleteBtn.innerHTML = '<i class="loading-spinner"></i> Deleting...';
    deleteBtn.disabled = true;

    fetch(`https://localhost:7181/api/Topics/${topicId}`, {
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
        Topics.showSuccessMessage(response.message);
        Topics.fetchTopics();
        Topics.clearContent();
      })
      .catch((err) => {
        console.error("Error:", err);
        Topics.showErrorMessage("Error deleting topic. Please try again.");
      })
      .finally(() => {
        deleteBtn.textContent = originalText;
        deleteBtn.disabled = false;
      });
  },

  showAddForm: async function () {
    const materials = await Topics.fetchMaterials();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Add New Topic</h3>
          <form id="topicForm" class="material-form">
            <div class="input-item">
              <label for="TopicName">Topic Name</label>
              <input type="text" id="TopicName" placeholder="Enter topic name" required maxlength="40" />
            </div>
            <div class="input-item">
              <label for="MaterialId">Material</label>
              <select id="MaterialId" required>
                <option value="">Select a material</option>
                ${materials
                  .map(
                    (material) =>
                      `<option value="${material.materialId}">${material.materialName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Topic</button>
              <button type="button" class="btn-cancel" onclick="Topics.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    document
      .getElementById("topicForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const data = {
          topicName: document.getElementById("TopicName").value,
          materialId: parseInt(document.getElementById("MaterialId").value),
        };

        fetch("https://localhost:7181/api/Topics", {
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
            const topic = {
              topicId: response.topicId,
              topicName: data.topicName,
              materialId: data.materialId,
              materialName:
                materials.find((m) => m.materialId === data.materialId)
                  ?.materialName || "N/A",
            };
            Topics.showSuccessMessage(response.message);
            Topics.addTopicToTree(topic);
            Topics.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            Topics.showErrorMessage("Error adding topic. Please try again.");
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  addTopicToTree: function (topic) {
    const topicsList = document.getElementById("topics-list");
    const li = document.createElement("li");
    li.className = "topic-item new-item";
    li.innerHTML = `
        <div class="material-content">
          <span class="material-name">${topic.topicName}</span>
          <span class="material-code">Material: ${
            topic.materialName || "N/A"
          }</span>
        </div>
      `;

    li.addEventListener("contextmenu", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Topics.showTopicContextMenu(e, topic);
    });

    li.addEventListener("dblclick", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Topics.showDetailsForm(topic);
    });

    li.addEventListener("click", (e) => {
      e.stopPropagation();
    });

    topicsList.appendChild(li);
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
            <button onclick="Topics.clearContent()" class="btn-back">Return to Home</button>
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
            <button onclick="Topics.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
  },
};

document.addEventListener("DOMContentLoaded", Topics.initialize);
