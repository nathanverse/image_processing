# Configure the Google Cloud Provider
terraform {
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
  required_version = ">= 1.0"
}

provider "google" {
  project = var.project_id
  region  = var.region
  zone    = var.zone
}

# Enable required APIs
resource "google_project_service" "pubsub_api" {
  service = "pubsub.googleapis.com"
  
  disable_dependent_services = true
}

resource "google_project_service" "storage_api" {
  service = "storage-component.googleapis.com"
  
  disable_dependent_services = true
}

# Create Pub/Sub topic for image processing events
resource "google_pubsub_topic" "image_processing_topic" {
  name = var.pubsub_topic_name
  
  message_retention_duration = "86400s"  # 24 hours
  
  labels = {
    environment = var.environment
    purpose     = "image-processing"
  }
  
  depends_on = [google_project_service.pubsub_api]
}

# Create subscription for image processing events
resource "google_pubsub_subscription" "image_processing_subscription" {
  name  = "${var.pubsub_topic_name}-subscription"
  topic = google_pubsub_topic.image_processing_topic.name
  
  ack_deadline_seconds = 300  # 5 minutes
  
  retry_policy {
    minimum_backoff = "10s"
    maximum_backoff = "600s"
  }
  
  dead_letter_policy {
    dead_letter_topic     = google_pubsub_topic.image_processing_dead_letter.id
    max_delivery_attempts = 5
  }
  
  labels = {
    environment = var.environment
    purpose     = "image-processing"
  }
}

# Create dead letter topic for failed messages
resource "google_pubsub_topic" "image_processing_dead_letter" {
  name = "${var.pubsub_topic_name}-dead-letter"
  
  labels = {
    environment = var.environment
    purpose     = "image-processing-dead-letter"
  }
  
  depends_on = [google_project_service.pubsub_api]
}

# Create service account for the application
resource "google_service_account" "image_processing_sa" {
  account_id   = "image-processing-sa"
  display_name = "Image Processing Service Account"
  description  = "Service account for image processing application"
}

# Grant Pub/Sub publisher role to service account
resource "google_pubsub_topic_iam_member" "publisher" {
  topic  = google_pubsub_topic.image_processing_topic.name
  role   = "roles/pubsub.publisher"
  member = "serviceAccount:${google_service_account.image_processing_sa.email}"
}

# Grant Pub/Sub subscriber role to service account
resource "google_pubsub_subscription_iam_member" "subscriber" {
  subscription = google_pubsub_subscription.image_processing_subscription.name
  role         = "roles/pubsub.subscriber"
  member       = "serviceAccount:${google_service_account.image_processing_sa.email}"
}

# Create service account key
resource "google_service_account_key" "image_processing_key" {
  service_account_id = google_service_account.image_processing_sa.name
  public_key_type    = "TYPE_X509_PEM_FILE"
}

# Storage bucket for processed images (optional)
resource "google_storage_bucket" "processed_images" {
  name     = "${var.project_id}-processed-images"
  location = var.region
  
  uniform_bucket_level_access = true
  
  versioning {
    enabled = true
  }
  
  lifecycle_rule {
    condition {
      age = 30
    }
    action {
      type = "Delete"
    }
  }
  
  labels = {
    environment = var.environment
    purpose     = "processed-images"
  }
  
  depends_on = [google_project_service.storage_api]
}

# Grant storage object admin role to service account
resource "google_storage_bucket_iam_member" "storage_admin" {
  bucket = google_storage_bucket.processed_images.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.image_processing_sa.email}"
}
