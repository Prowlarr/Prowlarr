import PropTypes from 'prop-types';
import React from 'react';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import HistoryDetails from './HistoryDetails';
import styles from './HistoryDetailsModal.css';

function getHeaderTitle(eventType) {
  switch (eventType) {
    case 'indexerQuery':
      return 'Indexer Query';
    case 'releaseGrabbed':
      return 'Release Grabbed';
    case 'indexerAuth':
      return 'Indexer Auth Attempt';
    case 'indexerRss':
      return 'Indexer Rss Query';
    default:
      return 'Unknown';
  }
}

function HistoryDetailsModal(props) {
  const {
    isOpen,
    eventType,
    indexer,
    data,
    isMarkingAsFailed,
    shortDateFormat,
    timeFormat,
    onMarkAsFailedPress,
    onModalClose
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {getHeaderTitle(eventType)}
        </ModalHeader>

        <ModalBody>
          <HistoryDetails
            eventType={eventType}
            indexer={indexer}
            data={data}
            shortDateFormat={shortDateFormat}
            timeFormat={timeFormat}
          />
        </ModalBody>

        <ModalFooter>
          {
            eventType === 'grabbed' &&
              <SpinnerButton
                className={styles.markAsFailedButton}
                kind={kinds.DANGER}
                isSpinning={isMarkingAsFailed}
                onPress={onMarkAsFailedPress}
              >
                Mark as Failed
              </SpinnerButton>
          }

          <Button
            onPress={onModalClose}
          >
            {translate('Close')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

HistoryDetailsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  eventType: PropTypes.string.isRequired,
  indexer: PropTypes.object.isRequired,
  data: PropTypes.object.isRequired,
  isMarkingAsFailed: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  onMarkAsFailedPress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

HistoryDetailsModal.defaultProps = {
  isMarkingAsFailed: false
};

export default HistoryDetailsModal;
